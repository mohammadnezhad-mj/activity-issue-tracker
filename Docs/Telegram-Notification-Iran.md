# نوتیفیکیشن تلگرام در ایران (دور زدن فیلتر)

از سرورهای داخل ایران به `api.telegram.org` دسترسی مستقیم وجود ندارد. دو روش پشتیبانی شده:

---

## روش ۱: پروکسی معکوس با Cloudflare Worker (پیشنهادی، رایگان)

سرور شما به آدرس Worker درخواست می‌زند؛ Worker درخواست را به تلگرام می‌فرستد و جواب را برمی‌گرداند.

### مرحله ۱: ساخت Worker در Cloudflare

1. وارد [Cloudflare Dashboard](https://dash.cloudflare.com) شوید.
2. **Workers & Pages** → **Create** → **Create Worker**.
3. نامی برای Worker انتخاب کنید (مثلاً `telegram-api-proxy`) و **Deploy**.
4. روی **Edit code** بروید و کد زیر را جایگزین کنید:

```javascript
export default {
  async fetch(request) {
    const url = new URL(request.url);
    url.hostname = "api.telegram.org";
    url.protocol = "https:";

    const modifiedRequest = new Request(url, {
      method: request.method,
      headers: request.headers,
      body: request.method !== "GET" && request.method !== "HEAD" ? request.body : null,
    });

    return fetch(modifiedRequest);
  },
};
```

5. **Save and Deploy**.

### مرحله ۲: تنظیم در برنامه

در `appsettings.json` فقط آدرس Worker را به‌عنوان پایه API قرار دهید:

```json
{
  "Telegram": {
    "BotToken": "توکن ربات از BotFather",
    "ChatId": "آیدی چت یا کانال",
    "AppBaseUrl": "https://your-site.com",
    "ApiBaseUrl": "https://telegram-api-proxy.حساب شما.workers.dev"
  }
}
```

`ApiBaseUrl` باید دقیقاً آدرس Worker شما باشد (بدون اسلش در انتها)، مثلاً:

- `https://telegram-api-proxy.my-subdomain.workers.dev`

بعد از این، درخواست‌های ارسال پیام به همین آدرس می‌روند و Worker آن‌ها را به `api.telegram.org` هدایت می‌کند.

---

## روش ۲: پروکسی HTTP

اگر یک پروکسی HTTP (یا SOCKS) دارید که به تلگرام دسترسی دارد، می‌توانید آدرس و در صورت نیاز نام کاربری/رمز را در تنظیمات بگذارید.

در `appsettings.json`:

```json
{
  "Telegram": {
    "BotToken": "...",
    "ChatId": "...",
    "AppBaseUrl": "...",
    "ProxyUrl": "http://proxy.example.com:8080",
    "ProxyUser": "username",
    "ProxyPassword": "password"
  }
}
```

- اگر پروکسی نیاز به احراز هویت ندارد، `ProxyUser` و `ProxyPassword` را خالی بگذارید یا حذف کنید.
- برای پروکسی SOCKS در ویندوز معمولاً باید از یک پروکسی HTTP جلوی آن استفاده شود یا از سرویس دیگری (مثل همان Worker) استفاده کنید.

---

## خلاصه تنظیمات Telegram در appsettings

| کلید | توضیح |
|------|--------|
| `BotToken` | توکن ربات از @BotFather |
| `ChatId` | آیدی چت/گروه/کانال (عددی یا @channel) |
| `AppBaseUrl` | آدرس پایه سایت برای لینک در پیام (اختیاری) |
| `ApiBaseUrl` | آدرس پایه API (مثلاً آدرس Cloudflare Worker) برای دور زدن فیلتر |
| `ProxyUrl` | آدرس پروکسی HTTP در صورت استفاده از روش ۲ |
| `ProxyUser` | نام کاربری پروکسی (اختیاری) |
| `ProxyPassword` | رمز پروکسی (اختیاری) |

اگر `ApiBaseUrl` خالی باشد، برنامه مستقیماً از `https://api.telegram.org` استفاده می‌کند (برای سرورهای خارج از ایران).
