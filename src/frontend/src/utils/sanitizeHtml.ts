import DOMPurify from 'dompurify';

const HTML_POLICY = {
  ALLOWED_TAGS: [
    'a',
    'b',
    'blockquote',
    'br',
    'caption',
    'code',
    'dd',
    'div',
    'dl',
    'dt',
    'em',
    'h1',
    'h2',
    'h3',
    'h4',
    'h5',
    'h6',
    'hr',
    'i',
    'li',
    'ol',
    'p',
    'pre',
    'span',
    'strong',
    'sub',
    'sup',
    'table',
    'tbody',
    'td',
    'th',
    'thead',
    'tr',
    'u',
    'ul',
  ],
  ALLOWED_ATTR: ['class', 'colspan', 'href', 'rowspan', 'title'],
  ALLOWED_URI_REGEXP: /^(?:(?:https?|mailto|tel):|\/(?!\/)|#)/i,
  FORBID_ATTR: ['style'],
  FORBID_TAGS: ['iframe', 'math', 'object', 'script', 'style', 'svg'],
};

export function sanitizeHtml(html: string): string {
  return DOMPurify.sanitize(html, HTML_POLICY);
}

export function sanitizeUrl(value: string | null | undefined): string | null {
  if (!value) return null;

  const candidate = value.trim();
  if (
    !candidate ||
    /[\u0000-\u001f\u007f]/.test(candidate) ||
    candidate.startsWith('//') ||
    candidate.startsWith('\\\\')
  ) {
    return null;
  }

  try {
    const url = new URL(candidate, window.location.origin);
    if (
      (url.protocol !== 'http:' && url.protocol !== 'https:') ||
      url.username ||
      url.password
    ) {
      return null;
    }

    return candidate;
  } catch {
    return null;
  }
}
