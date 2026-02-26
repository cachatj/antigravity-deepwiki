"use client"

import * as React from "react"
import { useTranslations } from "next-intl"
import { MessageCircle, X, Send, Loader2, Trash2, RefreshCw } from "lucide-react"
import { cn } from "@/lib/utils"

/**
 * 嵌入对话组件属性
 */
export interface EmbedChatWidgetProps {
  /** 应用ID */
  appId: string
  /** 自定义图标URL */
  iconUrl?: string
  /** 位置 */
  position?: 'bottom-right' | 'bottom-left' | 'top-right' | 'top-left'
  /** 主题 */
  theme?: 'light' | 'dark'
  /** API基础URL */
  apiBaseUrl?: string
}

/**
 * 嵌入配置响应
 */
interface EmbedConfig {
  valid: boolean
  errorCode?: string
  errorMessage?: string
  appName?: string
  iconUrl?: string
  availableModels: string[]
  defaultModel?: string
}

/**
 * 对话消息
 */
interface ChatMessage {
  id: string
  role: 'user' | 'assistant'
  content: string
  timestamp: number
}

/**
 * SSE事件
 */
interface SSEEvent {
  type: 'content' | 'tool_call' | 'tool_result' | 'done' | 'error'
  data: unknown
}

/**
 * 错误信息
 */
interface ErrorInfo {
  code?: string
  message: string
  retryable?: boolean
  retryAfterMs?: number
}

/**
 * 默认超时时间（毫秒）
 */
const DEFAULT_TIMEOUT_MS = 30000

/**
 * 默认重试次数
 */
const DEFAULT_MAX_RETRIES = 2

/**
 * 默认重试延迟（毫秒）
 */
const DEFAULT_RETRY_DELAY_MS = 1000

/**
 * 嵌入对话组件
 * 
 * 独立的悬浮球和对话面板，用于嵌入到外部网站
 * 使用应用配置的模型进行对话
 * 支持错误处理、超时和重试
 * 
 * Requirements: 14.5, 14.6, 11.1, 11.2, 11.3, 11.4
 */
export function EmbedChatWidget({
  appId,
  iconUrl: propIconUrl,
  position = 'bottom-right',
  theme = 'light',
  apiBaseUrl = '',
}: EmbedChatWidgetProps) {
  const t = useTranslations("chat")
  const [isOpen, setIsOpen] = React.useState(false)
  const [isLoading, setIsLoading] = React.useState(true)
  const [isEnabled, setIsEnabled] = React.useState(false)
  const [config, setConfig] = React.useState<EmbedConfig | null>(null)
  const [messages, setMessages] = React.useState<ChatMessage[]>([])
  const [input, setInput] = React.useState("")
  const [isSending, setIsSending] = React.useState(false)
  const [selectedModel, setSelectedModel] = React.useState<string>("")
  const [error, setError] = React.useState<ErrorInfo | null>(null)
  const [lastRequest, setLastRequest] = React.useState<{
    content: string
    userMessageId: string
    assistantMessageId: string
  } | null>(null)
  
  const messagesEndRef = React.useRef<HTMLDivElement>(null)
  const inputRef = React.useRef<HTMLTextAreaElement>(null)
  const abortControllerRef = React.useRef<AbortController | null>(null)

  // 获取图标URL
  const iconUrl = propIconUrl || config?.iconUrl

  // 加载配置
  React.useEffect(() => {
    const loadConfig = async () => {
      try {
        const url = `${apiBaseUrl}/api/v1/embed/config?appId=${encodeURIComponent(appId)}`
        const response = await fetch(url)
        const data: EmbedConfig = await response.json()
        
        if (data.valid) {
          setIsEnabled(true)
          setConfig(data)
          setSelectedModel(data.defaultModel || data.availableModels?.[0] || '')
        } else {
          console.error('[EmbedChatWidget] ' + t("embed.configInvalid"), data.errorMessage)
          setIsEnabled(false)
        }
      } catch (err) {
        console.error('[EmbedChatWidget] ' + t("embed.loadConfigFailed"), err)
        setIsEnabled(false)
      } finally {
        setIsLoading(false)
      }
    }

    loadConfig()
  }, [appId, apiBaseUrl])

  // 滚动到底部
  React.useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages])

  // 聚焦输入框
  React.useEffect(() => {
    if (isOpen && inputRef.current) {
      inputRef.current.focus()
    }
  }, [isOpen])

  // 组件卸载时取消请求
  React.useEffect(() => {
    return () => {
      if (abortControllerRef.current) {
        abortControllerRef.current.abort()
      }
    }
  }, [])

  // 生成消息ID
  const generateId = () => `msg-${Date.now()}-${Math.random().toString(36).slice(2, 11)}`

  // 切换面板
  const handleToggle = React.useCallback(() => {
    setIsOpen(prev => !prev)
  }, [])

  // 清空对话
  const handleClear = React.useCallback(() => {
    setMessages([])
    setError(null)
    setLastRequest(null)
  }, [])

  /**
   * 带超时的fetch请求
   */
  const fetchWithTimeout = async (
    url: string,
    options: RequestInit,
    timeoutMs: number
  ): Promise<Response> => {
    const controller = new AbortController()
    const timeoutId = setTimeout(() => controller.abort(), timeoutMs)
    
    try {
      const response = await fetch(url, {
        ...options,
        signal: controller.signal,
      })
      return response
    } finally {
      clearTimeout(timeoutId)
    }
  }

  /**
   * 延迟函数
   */
  const delay = (ms: number): Promise<void> => {
    return new Promise(resolve => setTimeout(resolve, ms))
  }

  // 发送消息
  const handleSend = React.useCallback(async () => {
    const content = input.trim()
    if (!content || isSending) return

    setError(null)
    setIsSending(true)

    // 创建新的AbortController
    abortControllerRef.current = new AbortController()

    // 添加用户消息
    const userMessage: ChatMessage = {
      id: generateId(),
      role: 'user',
      content,
      timestamp: Date.now(),
    }
    setMessages(prev => [...prev, userMessage])
    setInput("")

    // 添加助手消息占位
    const assistantMessage: ChatMessage = {
      id: generateId(),
      role: 'assistant',
      content: '',
      timestamp: Date.now(),
    }
    setMessages(prev => [...prev, assistantMessage])

    // 保存请求信息以便重试
    setLastRequest({
      content,
      userMessageId: userMessage.id,
      assistantMessageId: assistantMessage.id,
    })

    let retryCount = 0
    const maxRetries = DEFAULT_MAX_RETRIES
    const retryDelayMs = DEFAULT_RETRY_DELAY_MS

    while (retryCount <= maxRetries) {
      try {
        const url = `${apiBaseUrl}/api/v1/embed/stream`
        const allMessages = [...messages, userMessage]
        
        const response = await fetchWithTimeout(
          url,
          {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
            },
            body: JSON.stringify({
              appId,
              messages: allMessages.map(m => ({
                role: m.role,
                content: m.content,
              })),
              modelId: selectedModel,
            }),
          },
          DEFAULT_TIMEOUT_MS
        )

        if (!response.ok) {
          const isRetryable = response.status >= 500 || response.status === 429
          
          if (isRetryable && retryCount < maxRetries) {
            retryCount++
            await delay(retryDelayMs * retryCount)
            continue
          }
          
          throw new Error(`请求失败: ${response.status}`)
        }

        const reader = response.body?.getReader()
        if (!reader) {
          throw new Error('无法读取响应')
        }

        const decoder = new TextDecoder()
        let buffer = ''
        let assistantContent = ''

        while (true) {
          const { done, value } = await reader.read()
          if (done) break

          buffer += decoder.decode(value, { stream: true })
          const lines = buffer.split('\n')
          buffer = lines.pop() || ''

          for (const line of lines) {
            const trimmedLine = line.trim()
            if (!trimmedLine || trimmedLine.startsWith('event:')) continue

            if (trimmedLine.startsWith('data: ')) {
              const dataStr = trimmedLine.substring(6)
              try {
                const event: SSEEvent = JSON.parse(dataStr)
                
                if (event.type === 'content') {
                  assistantContent += event.data as string
                  setMessages(prev => 
                    prev.map(m => 
                      m.id === assistantMessage.id 
                        ? { ...m, content: assistantContent }
                        : m
                    )
                  )
                } else if (event.type === 'done') {
                  // 对话完成，清除重试信息
                  setLastRequest(null)
                } else if (event.type === 'error') {
                  const errorData = event.data as ErrorInfo
                  throw new Error(errorData.message || '对话失败')
                }
              } catch (parseError) {
                // 可能是纯文本内容
                if (typeof dataStr === 'string' && dataStr.trim()) {
                  assistantContent += dataStr
                  setMessages(prev => 
                    prev.map(m => 
                      m.id === assistantMessage.id 
                        ? { ...m, content: assistantContent }
                        : m
                    )
                  )
                }
              }
            }
          }
        }
        
        // 成功完成，退出重试循环
        break
        
      } catch (err) {
        // 处理超时错误
        if (err instanceof Error && err.name === 'AbortError') {
          if (retryCount < maxRetries) {
            retryCount++
            await delay(retryDelayMs * retryCount)
            continue
          }
          
          setError({
            message: '请求超时，请重试',
            code: 'REQUEST_TIMEOUT',
            retryable: true,
            retryAfterMs: retryDelayMs,
          })
          // 移除空的助手消息
          setMessages(prev => prev.filter(m => m.id !== assistantMessage.id))
          break
        }
        
        // 处理网络错误
        if (err instanceof TypeError && err.message.includes('fetch')) {
          if (retryCount < maxRetries) {
            retryCount++
            await delay(retryDelayMs * retryCount)
            continue
          }
          
          setError({
            message: '连接失败，请检查网络',
            code: 'CONNECTION_FAILED',
            retryable: true,
            retryAfterMs: retryDelayMs,
          })
          // 移除空的助手消息
          setMessages(prev => prev.filter(m => m.id !== assistantMessage.id))
          break
        }
        
        console.error('[EmbedChatWidget] 发送失败:', err)
        setError({
          message: err instanceof Error ? err.message : '发送失败，请重试',
          retryable: true,
        })
        // 移除空的助手消息
        setMessages(prev => prev.filter(m => m.id !== assistantMessage.id))
        break
      }
    }
    
    setIsSending(false)
    abortControllerRef.current = null
  }, [input, isSending, messages, appId, selectedModel, apiBaseUrl])

  // 重试发送
  const handleRetry = React.useCallback(() => {
    if (!lastRequest) return
    
    // 恢复输入状态
    setInput(lastRequest.content)
    setError(null)
    
    // 重新发送
    handleSend()
  }, [lastRequest, handleSend])

  // 处理键盘事件
  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      handleSend()
    }
  }

  // 获取位置样式
  const getPositionClasses = () => {
    const positions = {
      'bottom-right': 'right-6 bottom-6',
      'bottom-left': 'left-6 bottom-6',
      'top-right': 'right-6 top-6',
      'top-left': 'left-6 top-6',
    }
    return positions[position]
  }

  // 获取面板位置样式
  const getPanelPositionClasses = () => {
    const positions = {
      'bottom-right': 'right-6 bottom-24',
      'bottom-left': 'left-6 bottom-24',
      'top-right': 'right-6 top-24',
      'top-left': 'left-6 top-24',
    }
    return positions[position]
  }

  // 加载中或未启用时不显示
  if (isLoading || !isEnabled) {
    return null
  }

  const isDark = theme === 'dark'
  const canSend = input.trim() && !isSending

  return (
    <>
      {/* 悬浮球 */}
      <button
        type="button"
        onClick={handleToggle}
        className={cn(
          "fixed z-[99999] flex items-center justify-center",
          "w-14 h-14 rounded-full",
          "bg-gradient-to-br from-indigo-500 to-purple-600",
          "text-white shadow-lg",
          "transition-all duration-200 ease-in-out",
          "hover:scale-110 hover:shadow-xl",
          "active:scale-95",
          "focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2",
          getPositionClasses()
        )}
        aria-label={isOpen ? t("panel.close") : t("assistant.title")}
        aria-expanded={isOpen}
      >
        {isOpen ? (
          <X className="h-6 w-6" />
        ) : iconUrl ? (
          <img
            src={iconUrl}
            alt={t("assistant.title")}
            className="h-8 w-8 rounded-full object-cover"
          />
        ) : (
          <MessageCircle className="h-6 w-6" />
        )}
      </button>

      {/* 对话面板 */}
      {isOpen && (
        <div
          className={cn(
            "fixed z-[99998] flex flex-col",
            "w-[380px] h-[600px] max-h-[calc(100vh-120px)]",
            "rounded-xl shadow-2xl overflow-hidden",
            "transition-all duration-300 ease-in-out",
            isDark ? "bg-gray-900 text-white" : "bg-white text-gray-900",
            getPanelPositionClasses()
          )}
        >
          {/* 头部 */}
          <div
            className={cn(
              "flex items-center justify-between px-4 py-3 border-b",
              isDark ? "bg-gray-800 border-gray-700" : "bg-gray-50 border-gray-200"
            )}
          >
            <div className="flex items-center gap-3">
              <span className="font-semibold">
                {config?.appName || t("embed.title")}
              </span>
              {config?.availableModels && config.availableModels.length > 1 && (
                <select
                  value={selectedModel}
                  onChange={(e) => setSelectedModel(e.target.value)}
                  disabled={isSending}
                  className={cn(
                    "px-2 py-1 text-xs rounded border",
                    isDark 
                      ? "bg-gray-700 border-gray-600 text-white" 
                      : "bg-white border-gray-300"
                  )}
                >
                  {config.availableModels.map((model) => (
                    <option key={model} value={model}>
                      {model}
                    </option>
                  ))}
                </select>
              )}
            </div>
            <div className="flex items-center gap-1">
              <button
                type="button"
                onClick={handleClear}
                disabled={messages.length === 0}
                className={cn(
                  "p-1.5 rounded transition-colors",
                  isDark 
                    ? "hover:bg-gray-700 disabled:opacity-50" 
                    : "hover:bg-gray-200 disabled:opacity-50"
                )}
                title={t("panel.clearHistory")}
              >
                <Trash2 className="h-4 w-4" />
              </button>
              <button
                type="button"
                onClick={handleToggle}
                className={cn(
                  "p-1.5 rounded transition-colors",
                  isDark ? "hover:bg-gray-700" : "hover:bg-gray-200"
                )}
              >
                <X className="h-4 w-4" />
              </button>
            </div>
          </div>

          {/* 消息列表 */}
          <div className="flex-1 overflow-y-auto p-4 space-y-3">
            {messages.length === 0 ? (
              <div className="flex flex-col items-center justify-center h-full text-center text-gray-500">
                <div className="text-4xl mb-2">👋</div>
                <div className="font-medium mb-1">{t("embed.greeting")}</div>
                <div className="text-sm">{t("embed.greetingSubtitle")}</div>
              </div>
            ) : (
              messages.map((message) => (
                <div
                  key={message.id}
                  className={cn(
                    "max-w-[80%] px-4 py-2.5 rounded-2xl text-sm leading-relaxed break-words",
                    message.role === 'user'
                      ? "ml-auto bg-gradient-to-br from-indigo-500 to-purple-600 text-white rounded-br-sm"
                      : cn(
                          "mr-auto rounded-bl-sm",
                          isDark ? "bg-gray-700 text-gray-100" : "bg-gray-100 text-gray-900"
                        )
                  )}
                >
                  {message.role === 'assistant' && !message.content && isSending ? (
                    <div className="flex items-center gap-1">
                      <span className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style={{ animationDelay: '0ms' }} />
                      <span className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style={{ animationDelay: '150ms' }} />
                      <span className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style={{ animationDelay: '300ms' }} />
                    </div>
                  ) : (
                    <MessageContent content={message.content} isDark={isDark} />
                  )}
                </div>
              ))
            )}
            <div ref={messagesEndRef} />
          </div>

          {/* 错误提示 */}
          {error && (
            <div className="mx-4 mb-2 px-3 py-2 bg-red-50 text-red-600 text-sm rounded-lg">
              <div className="flex items-center justify-between">
                <span>{error.message}</span>
                <div className="flex items-center gap-2">
                  {error.retryable && lastRequest && (
                    <button
                      className="flex items-center gap-1 underline hover:no-underline"
                      onClick={handleRetry}
                      disabled={isSending}
                    >
                      <RefreshCw className="h-3 w-3" />
                      {t("panel.retry")}
                    </button>
                  )}
                  <button
                    className="underline hover:no-underline"
                    onClick={() => setError(null)}
                  >
                    {t("panel.closeError")}
                  </button>
                </div>
              </div>
            </div>
          )}

          {/* 输入区域 */}
          <div
            className={cn(
              "p-4 border-t flex items-end gap-2",
              isDark ? "border-gray-700" : "border-gray-200"
            )}
          >
            <textarea
              ref={inputRef}
              value={input}
              onChange={(e) => setInput(e.target.value)}
              onKeyDown={handleKeyDown}
              placeholder={t("embed.inputPlaceholder")}
              rows={1}
              disabled={isSending}
              className={cn(
                "flex-1 min-h-[40px] max-h-[120px] px-3 py-2.5",
                "border rounded-lg resize-none text-sm",
                "focus:outline-none focus:ring-2 focus:ring-indigo-500",
                isDark 
                  ? "bg-gray-800 border-gray-600 text-white placeholder-gray-400" 
                  : "bg-white border-gray-300 placeholder-gray-500"
              )}
              style={{
                height: 'auto',
                minHeight: '40px',
              }}
              onInput={(e) => {
                const target = e.target as HTMLTextAreaElement
                target.style.height = 'auto'
                target.style.height = Math.min(target.scrollHeight, 120) + 'px'
              }}
            />
            <button
              type="button"
              onClick={handleSend}
              disabled={!canSend}
              className={cn(
                "w-10 h-10 flex items-center justify-center rounded-lg",
                "bg-gradient-to-br from-indigo-500 to-purple-600 text-white",
                "transition-opacity",
                canSend ? "opacity-100" : "opacity-50 cursor-not-allowed"
              )}
            >
              {isSending ? (
                <Loader2 className="h-5 w-5 animate-spin" />
              ) : (
                <Send className="h-5 w-5" />
              )}
            </button>
          </div>
        </div>
      )}
    </>
  )
}

/**
 * 消息内容组件 - 简单的Markdown渲染
 */
function MessageContent({ content, isDark }: { content: string; isDark: boolean }) {
  if (!content) return null

  // 简单的Markdown处理
  const processedContent = React.useMemo(() => {
    let html = content
      // 转义HTML
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      // 代码块
      .replace(/```(\w*)\n([\s\S]*?)```/g, (_, _lang, code) => 
        `<pre class="my-2 p-3 rounded-md text-xs overflow-x-auto ${isDark ? 'bg-gray-800' : 'bg-gray-800 text-gray-100'}"><code>${code}</code></pre>`
      )
      // 行内代码
      .replace(/`([^`]+)`/g, `<code class="px-1.5 py-0.5 rounded text-xs ${isDark ? 'bg-gray-600' : 'bg-gray-200'}">$1</code>`)
      // 粗体
      .replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>')
      // 斜体
      .replace(/\*([^*]+)\*/g, '<em>$1</em>')
      // 链接
      .replace(/\[([^\]]+)\]\(([^)]+)\)/g, '<a href="$2" target="_blank" class="text-indigo-400 underline">$1</a>')
      // 换行
      .replace(/\n/g, '<br>')

    return html
  }, [content, isDark])

  return (
    <div 
      className="prose prose-sm max-w-none"
      dangerouslySetInnerHTML={{ __html: processedContent }} 
    />
  )
}
