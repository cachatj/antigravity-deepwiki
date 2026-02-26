import { getRequestConfig } from 'next-intl/server';

// English-only — all other locales removed for security and simplicity
export const locales = ['en'];

export default getRequestConfig(async ({ locale }) => {
  return {
    locale: 'en',
    messages: (await import(`./messages/en.json`)).default
  };
});
