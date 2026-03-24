/**
 * 对话助手 API 客户端
 * 
 * 实现SSE流式对话功能，包含错误处理、超时和重试机制
 * Requirements: 9.2, 11.1, 11.2, 11.3, 11.4
 */

import { getApiProxyUrl } from './env'
import { getToken } from './auth-api'
import { ChatMessage, ToolCall, ToolResult, QuotedText, ContentBlock, TokenUsage } from '@/hooks/use-chat-history'

// 重新导出类型以便其他模块使用
export type { ChatMessage, ToolCall, ToolResult, QuotedText }

const API_BASE_URL = getApiProxyUrl()

/** 默认请求超时时间（毫秒） */
const DEFAULT_TIMEOUT_MS = 30000

/** 默认重试次数 */
const DEFAULT_MAX_RETRIES = 2

/** 默认重试延迟（毫秒） */
const DEFAULT_RETRY_DELAY_MS = 1000

function buildApiUrl(path: string): string {
  if (!API_BASE_URL) {
    return path
  }
  const trimmedBase = API_BASE_URL.endsWith('/')
    ? API_BASE_URL.slice(0, -1)
    : API_BASE_URL
  return `${trimmedBase}${path}`
}

/**
 * 目录项
 */
export interface CatalogItem {
  title: string
  path: string
  children?: CatalogItem[]
}

/**
 * 文档上下文
 */
export interface DocContext {
  owner: string
  repo: string
  branch: string
  language: string
  currentDocPath: string
  catalogMenu: CatalogItem[]
  /** User's preferred language for AI responses */
  userLanguage?: string
}

/**
 * 对话请求消息DTO
 */
export interface ChatMessageDto {
  role: 'user' | 'assistant' | 'tool'
  content: string
  images?: string[]
  quotedText?: QuotedText
  toolCalls?: ToolCall[]
  toolResult?: ToolResult
}

/**
 * 分享消息 DTO
 */
export interface ChatShareMessage {
  id: string
  role: 'user' | 'assistant' | 'tool'
  content: string
  thinking?: string
  contentBlocks?: ContentBlock[]
  images?: string[]
  quotedText?: QuotedText
  toolCalls?: ToolCall[]
  toolResult?: ToolResult
  tokenUsage?: TokenUsage
  timestamp: number
}

/**
 * 创建分享请求载荷
 */
export interface CreateChatSharePayload {
  messages: ChatShareMessage[]
  context: DocContext
  modelId: string
  title?: string
  description?: string
  expireMinutes?: number
}

/**
 * 分享响应
 */
export interface ChatShareResponse {
  shareId: string
  title: string
  description?: string | null
  createdAt: string
  expiresAt?: string | null
  context: DocContext
  modelId: string
  messages: ChatShareMessage[]
}

/**
 * 对话请求
 */
export interface ChatRequest {
  messages: ChatMessageDto[]
  modelId: string
  context: DocContext
  appId?: string
}

/**
 * SSE event类型
 */
export type SSEEventType = 'content' | 'thinking' | 'tool_call' | 'tool_result' | 'done' | 'error'

/**
 * Thinking 事件数据
 */
export interface ThinkingEvent {
  type: 'start' | 'delta'
  content?: string
  index?: number
}

/**
 * ToolCall 事件数据（带 index）
 */
export interface ToolCallEvent {
  id: string
  name: string
  arguments?: Record<string, unknown> | null
  index?: number
}

/**
 * 错误信息
 */
export interface ErrorInfo {
  code: string
  message: string
  retryable?: boolean
  retryAfterMs?: number
}

/**
 * 错误码常量（与后端保持一致）
 * Requirements: 11.1, 11.2, 11.3
 */
export const ChatErrorCodes = {
  // 功能状态错误
  FEATURE_DISABLED: 'FEATURE_DISABLED',
  CONFIG_MISSING: 'CONFIG_MISSING',
  
  // 模型相关错误
  MODEL_UNAVAILABLE: 'MODEL_UNAVAILABLE',
  MODEL_CONFIG_INVALID: 'MODEL_CONFIG_INVALID',
  NO_AVAILABLE_MODELS: 'NO_AVAILABLE_MODELS',
  
  // 应用相关错误
  INVALID_APP_ID: 'INVALID_APP_ID',
  APP_MODEL_NOT_CONFIGURED: 'APP_MODEL_NOT_CONFIGURED',
  APP_DISABLED: 'APP_DISABLED',
  
  // 域名校验错误
  DOMAIN_NOT_ALLOWED: 'DOMAIN_NOT_ALLOWED',
  DOMAIN_UNKNOWN: 'DOMAIN_UNKNOWN',
  
  // 限流错误
  RATE_LIMIT_EXCEEDED: 'RATE_LIMIT_EXCEEDED',
  
  // 文档相关错误
  DOCUMENT_NOT_FOUND: 'DOCUMENT_NOT_FOUND',
  DOCUMENT_ACCESS_DENIED: 'DOCUMENT_ACCESS_DENIED',
  REPOSITORY_NOT_FOUND: 'REPOSITORY_NOT_FOUND',
  
  // 工具调用错误
  MCP_CALL_FAILED: 'MCP_CALL_FAILED',
  TOOL_EXECUTION_FAILED: 'TOOL_EXECUTION_FAILED',
  TOOL_NOT_FOUND: 'TOOL_NOT_FOUND',
  
  // 连接和超时错误
  CONNECTION_FAILED: 'CONNECTION_FAILED',
  REQUEST_TIMEOUT: 'REQUEST_TIMEOUT',
  STREAM_INTERRUPTED: 'STREAM_INTERRUPTED',
  
  // 内部错误
  INTERNAL_ERROR: 'INTERNAL_ERROR',
  UNKNOWN_ERROR: 'UNKNOWN_ERROR',
} as const

/**
 * 获取错误码对应的默认消息
 */
export function getErrorMessage(code: string): string {
  const messages: Record<string, string> = {
    [ChatErrorCodes.FEATURE_DISABLED]: '对话助手功能未启用',
    [ChatErrorCodes.CONFIG_MISSING]: '功能配置缺失',
    [ChatErrorCodes.MODEL_UNAVAILABLE]: '模型不可用，请选择其他模型',
    [ChatErrorCodes.MODEL_CONFIG_INVALID]: '模型配置无效',
    [ChatErrorCodes.NO_AVAILABLE_MODELS]: '暂无可用模型，请联系管理员配置',
    [ChatErrorCodes.INVALID_APP_ID]: 'Invalid app ID',
    [ChatErrorCodes.APP_MODEL_NOT_CONFIGURED]: 'App has no AI model configured',
    [ChatErrorCodes.APP_DISABLED]: 'App is disabled',
    [ChatErrorCodes.DOMAIN_NOT_ALLOWED]: 'Current domain is not in the allowed list',
    [ChatErrorCodes.DOMAIN_UNKNOWN]: 'Unable to determine request origin domain',
    [ChatErrorCodes.RATE_LIMIT_EXCEEDED]: 'Rate limit exceeded, please try again later',
    [ChatErrorCodes.DOCUMENT_NOT_FOUND]: 'Document not found',
    [ChatErrorCodes.DOCUMENT_ACCESS_DENIED]: 'Document access denied',
    [ChatErrorCodes.REPOSITORY_NOT_FOUND]: 'Repository not found',
    [ChatErrorCodes.MCP_CALL_FAILED]: 'MCP tool call failed',
    [ChatErrorCodes.TOOL_EXECUTION_FAILED]: 'Tool execution failed',
    [ChatErrorCodes.TOOL_NOT_FOUND]: 'Tool not found',
    [ChatErrorCodes.CONNECTION_FAILED]: 'Connection failed, please check your network',
    [ChatErrorCodes.REQUEST_TIMEOUT]: 'Request timed out, please try again',
    [ChatErrorCodes.STREAM_INTERRUPTED]: 'Data stream interrupted, please try again',
    [ChatErrorCodes.INTERNAL_ERROR]: 'Internal server error',
    [ChatErrorCodes.UNKNOWN_ERROR]: 'Unknown error',
  }
  return messages[code] || 'An error occurred'
}

/**
 * Determine if error is retryable
 */
export function isRetryableError(code: string): boolean {
  const retryableCodes: string[] = [
    ChatErrorCodes.CONNECTION_FAILED,
    ChatErrorCodes.REQUEST_TIMEOUT,
    ChatErrorCodes.STREAM_INTERRUPTED,
    ChatErrorCodes.RATE_LIMIT_EXCEEDED,
    ChatErrorCodes.INTERNAL_ERROR,
  ]
  return retryableCodes.includes(code)
}

/**
 * Completion info
 */
export interface DoneInfo {
  inputTokens: number
  outputTokens: number
}

/**
 * SSE event
 */
export interface SSEEvent {
  type: SSEEventType
  data: string | ThinkingEvent | ToolCall | ToolCallEvent | ToolResult | DoneInfo | ErrorInfo
}

/**
 * Model configuration
 */
export interface ModelConfig {
  id: string
  name: string
  provider: string
  isEnabled: boolean
}

/**
 * Chat assistant configuration
 */
export interface ChatAssistantConfig {
  isEnabled: boolean
  defaultModelId?: string
  enableImageUpload: boolean
}

/**
 * Parse SSE line
 */
function parseSSELine(line: string): SSEEvent | null {
  if (!line.startsWith('data: ')) {
    return null
  }
  
  const jsonStr = line.slice(6) // Remove "data: " prefix
  if (!jsonStr.trim()) {
    return null
  }
  
  try {
    return JSON.parse(jsonStr) as SSEEvent
  } catch {
    // If parsing fails, it may be plain text content
    return {
      type: 'content',
      data: jsonStr,
    }
  }
}

/**
 * Streaming chat options
 */
export interface StreamChatOptions {
  /** Timeout in milliseconds, default 30 seconds */
  timeoutMs?: number
  /** Maximum retries, default 2 */
  maxRetries?: number
  /** Retry delay in milliseconds, default 1 second */
  retryDelayMs?: number
  /** Cancel signal */
  signal?: AbortSignal
}

/**
 * Create fetch request with timeout
 */
async function fetchWithTimeout(
  url: string,
  options: RequestInit,
  timeoutMs: number
): Promise<Response> {
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
 * Delay function
 */
function delay(ms: number): Promise<void> {
  return new Promise(resolve => setTimeout(resolve, ms))
}

/**
 * SSE streaming chat
 * 
 * Uses async generator to parse SSE event stream
 * Supports timeout handling and auto-retry
 * 
 * @param request Chat request
 * @param options Streaming chat options
 * @yields SSEEvent events
 * 
 * Requirements: 9.2, 11.1, 11.2, 11.3, 11.4
 */
export async function* streamChat(
  request: ChatRequest,
  options: StreamChatOptions = {}
): AsyncGenerator<SSEEvent> {
  const {
    timeoutMs = DEFAULT_TIMEOUT_MS,
    maxRetries = DEFAULT_MAX_RETRIES,
    retryDelayMs = DEFAULT_RETRY_DELAY_MS,
    signal,
  } = options
  
  const url = buildApiUrl('/api/v1/chat/stream')
  
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
  }
  
  const token = getToken()
  if (token) {
    headers['Authorization'] = `Bearer ${token}`
  }
  
  let lastError: ErrorInfo | null = null
  let retryCount = 0
  
  while (retryCount <= maxRetries) {
    // Check if cancelled
    if (signal?.aborted) {
      yield {
        type: 'error',
        data: {
          code: 'ABORTED',
          message: 'Request cancelled',
          retryable: false,
        },
      }
      return
    }
    
    try {
      const response = await fetchWithTimeout(
        url,
        {
          method: 'POST',
          headers,
          body: JSON.stringify(request),
        },
        timeoutMs
      )
      
      if (!response.ok) {
        let errorMessage = 'Request failed'
        let errorCode = `HTTP_${response.status}`
        
        try {
          const errorData = await response.json()
          errorMessage = errorData.message || errorData.error || errorMessage
          errorCode = errorData.code || errorCode
        } catch {
          errorMessage = await response.text() || errorMessage
        }
        
        const isRetryable = response.status >= 500 || response.status === 429
        
        if (isRetryable && retryCount < maxRetries) {
          lastError = { code: errorCode, message: errorMessage, retryable: true }
          retryCount++
          await delay(retryDelayMs * retryCount) // Exponential backoff
          continue
        }
        
        yield {
          type: 'error',
          data: {
            code: errorCode,
            message: errorMessage,
            retryable: isRetryable,
          },
        }
        return
      }
      
      const reader = response.body?.getReader()
      if (!reader) {
        yield {
          type: 'error',
          data: {
            code: 'NO_BODY',
            message: 'Response body is empty',
            retryable: false,
          },
        }
        return
      }
      
      const decoder = new TextDecoder()
      let buffer = ''
      
      try {
        while (true) {
          // Check if cancelled
          if (signal?.aborted) {
            reader.releaseLock()
            yield {
              type: 'error',
              data: {
                code: 'ABORTED',
                message: 'Request cancelled',
                retryable: false,
              },
            }
            return
          }
          
          const { done, value } = await reader.read()
          
          if (done) {
            break
          }
          
          buffer += decoder.decode(value, { stream: true })
          
          // Split and process by lines
          const lines = buffer.split('\n')
          buffer = lines.pop() || '' // Keep the last incomplete line
          
          for (const line of lines) {
            const trimmedLine = line.trim()
            if (!trimmedLine) {
              continue
            }
            
            const event = parseSSELine(trimmedLine)
            if (event) {
              yield event
              
              // If completion or error event, end stream
              if (event.type === 'done' || event.type === 'error') {
                return
              }
            }
          }
        }
        
        // Process remaining buffer
        if (buffer.trim()) {
          const event = parseSSELine(buffer.trim())
          if (event) {
            yield event
          }
        }
        
        // Successfully completed, exit retry loop
        return
        
      } finally {
        reader.releaseLock()
      }
      
    } catch (err) {
      // Handle timeout error
      if (err instanceof Error && err.name === 'AbortError') {
        const errorInfo: ErrorInfo = {
          code: ChatErrorCodes.REQUEST_TIMEOUT,
          message: 'Request timed out, please try again',
          retryable: true,
          retryAfterMs: retryDelayMs,
        }
        
        if (retryCount < maxRetries) {
          lastError = errorInfo
          retryCount++
          await delay(retryDelayMs * retryCount)
          continue
        }
        
        yield {
          type: 'error',
          data: errorInfo,
        }
        return
      }
      
      // Handle network error
      if (err instanceof TypeError && err.message.includes('fetch')) {
        const errorInfo: ErrorInfo = {
          code: ChatErrorCodes.CONNECTION_FAILED,
          message: 'Connection failed, please check your network',
          retryable: true,
          retryAfterMs: retryDelayMs,
        }
        
        if (retryCount < maxRetries) {
          lastError = errorInfo
          retryCount++
          await delay(retryDelayMs * retryCount)
          continue
        }
        
        yield {
          type: 'error',
          data: errorInfo,
        }
        return
      }
      
      // Other errors
      console.error('Chat failed:', err)
      yield {
        type: 'error',
        data: {
          code: ChatErrorCodes.UNKNOWN_ERROR,
          message: err instanceof Error ? err.message : 'Chat failed, please try again',
          retryable: false,
        },
      }
      return
    }
  }
  
  // Retries exhausted
  if (lastError) {
    yield {
      type: 'error',
      data: {
        ...lastError,
        message: `${lastError.message} (retried ${maxRetries} times)`,
        retryable: true,
      },
    }
  }
}

/**
 * Get chat assistant configuration
 */
export async function getChatConfig(): Promise<ChatAssistantConfig> {
  const url = buildApiUrl('/api/v1/chat/config')
  
  const headers: Record<string, string> = {}
  const token = getToken()
  if (token) {
    headers['Authorization'] = `Bearer ${token}`
  }
  
  const response = await fetch(url, { headers })
  
  if (!response.ok) {
    throw new Error('Failed to fetch configuration')
  }
  
  return response.json()
}

/**
 * Convert ChatMessage to ChatShareMessage
 */
export function toChatShareMessage(message: ChatMessage): ChatShareMessage {
  return {
    id: message.id,
    role: message.role,
    content: message.content,
    thinking: message.thinking,
    contentBlocks: message.contentBlocks ? message.contentBlocks.map(block => ({ ...block })) : undefined,
    images: message.images ? [...message.images] : undefined,
    quotedText: message.quotedText ? { ...message.quotedText } : undefined,
    toolCalls: message.toolCalls ? message.toolCalls.map(call => ({ ...call })) : undefined,
    toolResult: message.toolResult ? { ...message.toolResult } : undefined,
    tokenUsage: message.tokenUsage ? { ...message.tokenUsage } : undefined,
    timestamp: message.timestamp,
  }
}

/**
 * Create chat share
 */
export async function createChatShare(payload: CreateChatSharePayload): Promise<ChatShareResponse> {
  const url = buildApiUrl('/api/v1/chat/share')
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
  }

  const token = getToken()
  if (token) {
    headers['Authorization'] = `Bearer ${token}`
  }

  const response = await fetch(url, {
    method: 'POST',
    headers,
    body: JSON.stringify(payload),
  })

  if (!response.ok) {
    throw new Error(await extractErrorMessage(response, 'Failed to create share'))
  }

  return response.json()
}

/**
 * Get share details
 */
export async function getChatShare(shareId: string, init?: RequestInit): Promise<ChatShareResponse> {
  const url = buildApiUrl(`/api/v1/chat/share/${shareId}`)
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
  }

  const response = await fetch(url, {
    method: 'GET',
    headers,
    cache: 'no-store',
    ...init,
  })

  if (!response.ok) {
    throw new Error(await extractErrorMessage(response, 'Share not found or expired'))
  }

  return response.json()
}

/**
 * Revoke share
 */
export async function revokeChatShare(shareId: string): Promise<void> {
  const url = buildApiUrl(`/api/v1/chat/share/${shareId}`)
  const headers: Record<string, string> = {}
  const token = getToken()
  if (token) {
    headers['Authorization'] = `Bearer ${token}`
  }

  const response = await fetch(url, {
    method: 'DELETE',
    headers,
  })

  if (!response.ok && response.status !== 404) {
    throw new Error(await extractErrorMessage(response, 'Failed to revoke share'))
  }
}

async function extractErrorMessage(response: Response, fallback: string): Promise<string> {
  try {
    const data = await response.json()
    return data?.message || data?.error || fallback
  } catch {
    try {
      const text = await response.text()
      return text || fallback
    } catch {
      return fallback
    }
  }
}

/**
 * Get available model list
 */
export async function getAvailableModels(): Promise<ModelConfig[]> {
  const url = buildApiUrl('/api/v1/chat/models')
  
  const headers: Record<string, string> = {}
  const token = getToken()
  if (token) {
    headers['Authorization'] = `Bearer ${token}`
  }
  
  const response = await fetch(url, { headers })
  
  if (!response.ok) {
    throw new Error('Failed to fetch model list')
  }
  
  return response.json()
}

/**
 * Convert ChatMessage to ChatMessageDto
 */
export function toChatMessageDto(message: ChatMessage): ChatMessageDto {
  return {
    role: message.role,
    content: message.content,
    images: message.images,
    quotedText: message.quotedText,
    toolCalls: message.toolCalls,
    toolResult: message.toolResult,
  }
}
