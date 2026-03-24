import common from '@/i18n/messages/en/common.json';
import theme from '@/i18n/messages/en/theme.json';
import sidebar from '@/i18n/messages/en/sidebar.json';
import auth from '@/i18n/messages/en/auth.json';

type Messages = {
  common: typeof common;
  theme: typeof theme;
  sidebar: typeof sidebar;
  auth: typeof auth;
};

declare global {
  // eslint-disable-next-line @typescript-eslint/no-empty-object-type
  interface IntlMessages extends Messages {}
}
