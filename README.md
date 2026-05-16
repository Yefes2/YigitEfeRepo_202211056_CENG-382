# GrubBytes

A full-stack catering web application built with ASP.NET Core MVC (.NET 10) for CENG 382 Web Development.

GrubBytes is a street food ordering platform where customers can browse menus, place orders, and rate caterers. Caterers manage their menus and incoming orders. Administrators oversee the entire system.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core MVC (.NET 10) |
| Authentication | ASP.NET Core Identity |
| ORM | Entity Framework Core 10.0.7 (Code First) |
| Database | SQL Server LocalDB |
| Email | MailKit (Gmail SMTP) |
| PDF | QuestPDF (Community License) |
| Maps | Leaflet.js + OpenStreetMap |
| Charts | Chart.js 4.4.0 |
| CAPTCHA | Google reCAPTCHA v2 |

---

## Prerequisites

Before running the project, make sure you have the following installed:

- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- [SQL Server LocalDB](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb) (included with Visual Studio)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or later (recommended), or VS Code with the C# extension
- [EF Core CLI tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) — install with:

```bash
dotnet tool install --global dotnet-ef
```

---

## Setup Instructions

### 1. Clone the repository

```bash
git clone https://github.com/Yefes2/YigitEfeRepo_202211056_CENG-382.git
cd YigitEfeRepo_202211056_CENG-382/GrubBytes
```

### 2. Restore dependencies

```bash
dotnet restore
```

### 3. Configure the database

The connection string in `appsettings.json` points to SQL Server LocalDB and should work out of the box on any Windows machine with LocalDB installed:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=GrubBytesDb;Trusted_Connection=True;"
}
```

Apply the migrations to create the database:

```bash
dotnet ef database update
```

### 4. Configure secrets (Email and reCAPTCHA)

Sensitive credentials are stored using ASP.NET Core User Secrets and are **not committed to the repository**. You need to set them manually.

#### Email (Gmail SMTP)

You need a Gmail account with a 2-Step Verification enabled App Password:

```bash
dotnet user-secrets set "EmailSettings:SenderEmail" "your-gmail@gmail.com"
dotnet user-secrets set "EmailSettings:AppPassword" "xxxx xxxx xxxx xxxx"
```

> If you skip this step, the app will still run. Email sending will fail silently and the error will be logged — it will not crash the application.

#### reCAPTCHA v2

Register a site at [google.com/recaptcha](https://www.google.com/recaptcha), add `localhost` as a domain, and get your site key and secret key:

```bash
dotnet user-secrets set "ReCaptcha:SiteKey" "your-site-key"
dotnet user-secrets set "ReCaptcha:SecretKey" "your-secret-key"
```

> If you skip this step, the CAPTCHA widget will not render and login/register will be blocked by the server-side verification check. You can temporarily disable the CAPTCHA check in `AccountController.cs` for local testing if needed.

### 5. Add food images

Copy the three food images into the following folder (create it if it does not exist):

```
GrubBytes/wwwroot/uploads/menu/
```

Expected filenames:
- `spicy-chicken-wrap.jpg`
- `smash-burger.jpg`
- `street-fries.jpg`

> Images are not committed to the repository. Without them the menu cards will render without images but the application will function normally.

### 6. Run the application

```bash
dotnet run
```

The app will start on `https://localhost:{port}`. On first run, the database is seeded automatically with the following test accounts:

| Role | Email | Password |
|---|---|---|
| Admin | admin@grubbytes.com | Admin123! |
| Caterer | caterer@grubbytes.com | Caterer123! |
| User | Register via /Account/Register | min 6 chars + 1 digit |

---

## Database

To regenerate the database from scratch:

```bash
dotnet ef database drop
dotnet ef database update
```

A full SQL generation script is included in the repository at:

```
GrubBytes/GrubBytes_Database_Script.sql
```

This script was generated with `dotnet ef migrations script --idempotent` and can be run directly against SQL Server to reproduce the schema without the EF CLI tools.

---

## Project Structure

```
GrubBytes/
├── Controllers/       — AccountController, AdminController, CartController,
│                        CatererController, HomeController, UserController
├── Data/              — AppDbContext (EF Core DbContext)
├── Migrations/        — InitialCreate, AddFavorites, RecreateWithFavorites
├── Models/            — All entity classes + CartItem (session model)
├── Services/          — CartService, EmailService, LogService, PdfService
├── ViewModels/        — LoginViewModel, MenuItemViewModel,
│                        PaymentViewModel, RegisterViewModel
├── Views/             — Razor views organised by controller
├── wwwroot/           — CSS, JS, uploaded images
├── appsettings.json   — Non-sensitive config
└── Program.cs         — DI registration, middleware, seeding
```

---

## Features

**Users**
- Browse menu with search and price range filter
- Session-based cart with quantity management
- Payment simulation with card expiry validation
- Order history with PDF receipt and agreement download
- Rate completed orders (food + caterer score)
- Reorder from history, favorite/bookmark items
- User dashboard with stats and favorites grid

**Caterers**
- Dashboard with revenue charts (Chart.js)
- Menu management with availability toggle
- Order management with status updates

**Admins**
- System dashboard with platform-wide stats
- User management with lock/unlock
- Order status management
- System logs with filtering and CSV export

**Global**
- Dark / light mode with localStorage persistence
- Google reCAPTCHA v2 on login and register
- Forgot password with email reset link
- Password strength indicator
- Toast notifications, loading spinners, back to top button
- Interactive NearMe map (Leaflet.js + OpenStreetMap)

---

## Links

- **GitHub:** https://github.com/Yefes2/YigitEfeRepo_202211056_CENG-382
- **Demo Video:** [See links.txt]

---

## Student

**Yiğit Efe** — 202211056  
CENG 382 Web Development  
May 2026
