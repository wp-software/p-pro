import { describe, expect, it } from 'vitest';
import { sanitizeHtml, sanitizeUrl } from '@/utils/sanitizeHtml';

describe('sanitizeHtml', () => {
  it('removes script tags, event handlers, and javascript links', () => {
    const sanitized = sanitizeHtml(
      '<p onclick="run()">safe</p><img src=x onerror=run()><a href="javascript:run()">link</a><script>run()</script>'
    );

    expect(sanitized).toContain('<p>safe</p>');
    expect(sanitized).toContain('link</a>');
    expect(sanitized).not.toMatch(/<script|onerror|onclick|javascript:/i);
  });
});

describe('sanitizeUrl', () => {
  it('allows http(s) and relative URLs', () => {
    expect(sanitizeUrl('https://files.example.test/report.pdf')).toBe(
      'https://files.example.test/report.pdf'
    );
    expect(sanitizeUrl('/storage/report.pdf')).toBe('/storage/report.pdf');
  });

  it.each(['javascript:alert(1)', '//evil.example/path', '\\\\evil.example\\path'])(
    'rejects unsafe URL %s',
    (url) => {
      expect(sanitizeUrl(url)).toBeNull();
    }
  );
});
