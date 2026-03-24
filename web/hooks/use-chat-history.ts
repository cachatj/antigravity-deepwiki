import * as React from 'react'

/**
 * Tool call information
 */
export interface ToolCall {
  id: string
  name: string
  arguments: Record<string, unknown>
}

/**
 * Tool execution result
 */
export interface ToolResult {
  toolCallId: string
  result: string
  isError: boolean
}

/**
 * Quoted selected text
 */
export interface QuotedText {
  title?: string
  text: string
}

/**
 * Token usage statistics
 */
export interface TokenUsage {
  inputTokens: number
  outputTokens: number
}

/**
 * Content block type
 */
export type ContentBlockType = 'thinking' | 'text' | 'tool_call'

/**
 * Content block
 */
export interface ContentBlock {
  type: ContentBlockType
  content?: string           // thinking or text content
  toolCall?: ToolCall        // Tool call info for tool_call type
}

/**
 * Chat message
 */
export interface ChatMessage {
  id: string
  role: 'user' | 'assistant' | 'tool'
  content: string
  thinking?: string          // AI thinking content
  contentBlocks?: ContentBlock[]  // Content blocks stored in order
  images?: string[]          // Base64-encoded images
  quotedText?: QuotedText    // Quoted selected text
  toolCalls?: ToolCall[]     // Tool calls (backward compatible)
  toolResult?: ToolResult    // Tool result
  tokenUsage?: TokenUsage    // Token usage statistics
  timestamp: number
}

/**
 * New message input (excludes auto-generated fields)
 */
export type NewChatMessage = Omit<ChatMessage, 'id' | 'timestamp'>

/**
 * Message update
 */
export type ChatMessageUpdate = Partial<Omit<ChatMessage, 'id' | 'timestamp'>>

/**
 * useChatHistory Hook return type
 */
export interface UseChatHistoryReturn {
  messages: ChatMessage[]
  addMessage: (msg: NewChatMessage) => string
  updateMessage: (id: string, updates: ChatMessageUpdate) => void
  clearHistory: () => void
}

/**
 * Generate unique message ID
 */
function generateMessageId(): string {
  return `msg_${Date.now()}_${Math.random().toString(36).substring(2, 9)}`
}

/**
 * Chat history management Hook
 * 
 * Features:
 * - 维护完整的对话历史（用户消息、AI回复、工具调用和Tool result）
 * - Supports adding, updating, and clearing messages
 * - Auto-clears on page refresh (not persisted)
 * 
 * @returns UseChatHistoryReturn
 * 
 * Requirements: 8.1, 8.2, 8.4
 */
export function useChatHistory(): UseChatHistoryReturn {
  const [messages, setMessages] = React.useState<ChatMessage[]>([])

  /**
   * Add new message to history
   * @param msg New message (excludes id and timestamp)
   * @returns Generated message ID
   */
  const addMessage = React.useCallback((msg: NewChatMessage): string => {
    const id = generateMessageId()
    const newMessage: ChatMessage = {
      ...msg,
      id,
      timestamp: Date.now(),
    }
    setMessages(prev => [...prev, newMessage])
    return id
  }, [])

  /**
   * Update message by ID
   * @param id Message ID
   * @param updates Fields to update
   */
  const updateMessage = React.useCallback((id: string, updates: ChatMessageUpdate): void => {
    setMessages(prev =>
      prev.map(msg =>
        msg.id === id ? { ...msg, ...updates } : msg
      )
    )
  }, [])

  /**
   * Clear all chat history
   */
  const clearHistory = React.useCallback((): void => {
    setMessages([])
  }, [])

  return {
    messages,
    addMessage,
    updateMessage,
    clearHistory,
  }
}
