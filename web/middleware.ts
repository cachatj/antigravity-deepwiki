import { NextRequest, NextResponse } from 'next/server';

const supportedLocales = ['en'];

export function middleware(request: NextRequest) {
  // Get language from URL query parameter (used for repository doc pages)
  const urlLang = request.nextUrl.searchParams.get('lang');
  
  // Get language from cookie
  const cookieLocale = request.cookies.get('NEXT_LOCALE')?.value;
  
  // Priority: URL lang parameter > cookie > default 'en'
  let locale = 'en';
  if (urlLang && supportedLocales.includes(urlLang)) {
    locale = urlLang;
  } else if (cookieLocale && supportedLocales.includes(cookieLocale)) {
    locale = cookieLocale;
  }
  
  // Add locale to request headers for i18n config
  const requestHeaders = new Headers(request.headers);
  requestHeaders.set('x-next-intl-locale', locale);

  return NextResponse.next({
    request: {
      headers: requestHeaders,
    },
  });
}

export const config = {
  matcher: ['/((?!api|_next|_vercel|.*\\..*).*)'],
};
