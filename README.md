# الواحة العربية للتأمين

Arabic, right-to-left website for Al Waha Arabia Insurance. Customers browse news and send an insurance request. Staff review requests, issue compulsory motor policies through IMS, and follow payments through a queue.

## What is included

- Public home, news, and contact pages
- Insurance request form for products that stay inside this site
- Separate pages for travelers, the orange Arab card, and compulsory motor
- Compulsory motor lookups, premium preview, and issue through the IMS LISA API
- Staff dashboard, date reports, and live notifications when a customer sends a request
- Payment queue with Redis and RabbitMQ. The bank gateway is not connected yet, so a queued payment is not charged

## Requirements

- .NET 9
- SQL Server
- Redis on port 6379
- RabbitMQ on port 5672

Start Redis with:

```bash
docker compose up -d redis
```

RabbitMQ on this machine is the Windows service. Production needs both Redis and RabbitMQ on the server that hosts this website.

## Run

```bash
dotnet run
```

The site listens on `http://localhost:5101`. The database is `AwaiDb` on the local SQL Server, from `ConnectionStrings:DbCon` in `appsettings.json`. Migrations run on startup.

Staff emails are in `appsettings.json`. Passwords are not. On this machine store them with user secrets:

```bash
dotnet user-secrets set "SeedProg:Password" "<password>"
dotnet user-secrets set "SeedAdmin:Password" "<password>"
dotnet user-secrets set "Ims:ApiKey" "<key>"
dotnet user-secrets set "Smtp:UserName" "awain2026@gmail.com"
dotnet user-secrets set "Smtp:From" "awain2026@gmail.com"
dotnet user-secrets set "Smtp:Password" "<gmail-app-password>"
```

On the production server, set the same values as environment variables: `SeedProg__Password`, `SeedAdmin__Password`, `Ims__ApiKey`, `Smtp__UserName`, `Smtp__Password`, and `Smtp__From`. Do not put them in a file that is committed. Gmail often needs an App Password, not the normal login password.

## IMS key

The compulsory-motor API key is read from configuration (`Ims:ApiKey`). Locally that value comes from user secrets. In production it comes from the `Ims__ApiKey` environment variable. The key is sent in a header, not in the page address. The IMS base address is `Ims:BaseUrl` in `appsettings.json`.

## Payment flow

A pay request keeps one idempotency key. Redis blocks a second click with the same key, and the database stores one payment row. RabbitMQ delivers that payment to a background worker. If the worker stops, the payment returns to the queue. After five attempts it waits for a person to review it. The same key is what the bank will use later so a retry does not capture the money twice.
