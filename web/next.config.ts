import type { NextConfig } from "next";
import createNextIntlPlugin from 'next-intl/plugin';

const withNextIntl = createNextIntlPlugin('./i18n/request.ts');

const nextConfig: NextConfig = {
  output: 'standalone',
  // API proxying is now forwarded dynamically at runtime; see app/api/[...path]/route.ts
  // The API_PROXY_URL environment variable is read at runtime, no need to bake it in at build time
};

export default withNextIntl(nextConfig);
