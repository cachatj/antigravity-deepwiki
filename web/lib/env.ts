
// 缓存环境变量和上次加载时间
let cachedApiUrl: string | null = null;
let lastLoadTime = 0;
const CACHE_TTL = 5000; // 5秒缓存，方便热更新

/**
 * 获取 API 代理地址
 */
export function getApiProxyUrl(): string {
  
  // 优先使用系统环境变量（Docker/K8s 传入）
  if (process.env.API_PROXY_URL) {
    return process.env.API_PROXY_URL;
  }
  
  const now = Date.now();
  // 使用缓存
  if (cachedApiUrl !== null && (now - lastLoadTime) < CACHE_TTL) {
    return cachedApiUrl;
  }
  
  try {
    // 使用 require 避免构建时打包 Node.js 模块
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const fs = require('fs');
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const path = require('path');
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const dotenv = require('dotenv');
    
    // 动态读取 .env 文件
    const rootDir = process.cwd();
    const envLocalPath = path.resolve(rootDir, '.env.local');
    const envPath = path.resolve(rootDir, '.env');
    
    // 优先加载 .env.local
    if (fs.existsSync(envLocalPath)) {
      const result = dotenv.config({ path: envLocalPath });
      if (result.parsed?.API_PROXY_URL) {
        cachedApiUrl = result.parsed.API_PROXY_URL;
        lastLoadTime = now;
        return cachedApiUrl!;
      }
    }
    
    // 其次加载 .env
    if (fs.existsSync(envPath)) {
      const result = dotenv.config({ path: envPath });
      if (result.parsed?.API_PROXY_URL) {
        cachedApiUrl = result.parsed.API_PROXY_URL;
        lastLoadTime = now;
        return cachedApiUrl!;
      }
    }
  } catch {
    // 模块加载失败
  }
  
  cachedApiUrl = '';
  lastLoadTime = now;
  return '';
}
