# Marin3

# 📝 Installation Instructions

**Last Updated:** {{today’s date}}

---

## 📋 Prerequisites

* An Ubuntu VPS (tested on 20.04+ or 22.04).
* Root or sudo access.
* Domain name `app.marin.cr` pointing to your VPS public IP (DNS A record).
* Git installed locally or ability to SSH into the VPS.
* Postgres already installed and accessible (internal or external).

---

## 1. Prepare the VPS

Update and install required tools:

```bash
sudo apt-get update && sudo apt-get -y upgrade
sudo apt-get -y install git ufw curl
```

Install Docker + Docker Compose plugin:

```bash
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker $USER
```

*(Log out/in to apply docker group permissions.)*

---

## 2. Set up project directory

Create a root folder for the app:

```bash
sudo mkdir -p /root/marinapp
cd /root/marinapp
```

Clone the GitHub repository:

```bash
git clone https://github.com/marinoscar/Marin3
```

Your app code now lives in:

```
/root/marinapp/Marin3/src
```

---

## 3. Data Protection keys directory

Create a folder to persist ASP.NET Core Data Protection keys (used for cookie encryption):

```bash
mkdir -p /root/marinapp/.dpkeys
```

---

## 4. Configure environment variables

Create `.env`:

```bash
nano /root/marinapp/.env
```

Paste (adjust Postgres connection string):

```

MARIN_APP_DB_PASSWORD=<your_password>
MARIN_APP_DB_USER=<user>
MARIN_APP_DB_SERVER=<server>
MARIN_APP_DB_PORT=5432
ASPNETCORE_URLS=http://+:9095

```

Save and exit (`Ctrl+O`, `Enter`, `Ctrl+X`).

---

## 5. Create Docker Compose file

Open:

```bash
nano /root/marinapp/docker-compose.yml
```

Paste:

```yaml

services:
  init-perms:
    image: busybox
    command: sh -c "mkdir -p /keys && chown -R 1654:1654 /keys"
    volumes:
      - dpkeys:/keys
    restart: "no"

  marinapp:
    # If DOCKER_REGISTRY is unset, 'marinapp' is used
    image: ${DOCKER_REGISTRY-}marinapp
    build:
      context: /root/marinapp/Marin3/src
      dockerfile: MarinApp/Dockerfile
    env_file:
      - .env
    volumes:
      - dpkeys:/home/app/.aspnet/DataProtection-Keys   # persistent key ring
    ports:
      - "9095:9095"
      - "9096:9096"
    environment:
      # ---- Option A: keep MARIN_* (uses your current code) ----
      MARIN_APP_DB_SERVER: ${MARIN_APP_DB_SERVER}
      MARIN_APP_DB_PORT: ${MARIN_APP_DB_PORT}
      MARIN_APP_DB_USER: ${MARIN_APP_DB_USER}
      MARIN_APP_DB_PASSWORD: ${MARIN_APP_DB_PASSWORD}
      ASPNETCORE_URLS: ${ASPNETCORE_URLS}

      # ---- Option B (recommended): comment Option A and use this instead ----
      # ConnectionStrings__Default: ${ConnectionStrings__Default}
      # ASPNETCORE_URLS: ${ASPNETCORE_URLS}

    depends_on:
      - init-perms
    restart: unless-stopped

volumes:
  dpkeys:

```

Save and exit.

---

## 6. Build and start the app

```bash
cd /root/marinapp
docker compose build
docker compose up -d
```

Check logs:

```bash
docker compose logs -f --tail=200
```

Test locally on the VPS:

```bash
curl -i http://127.0.0.1:9095/
```

---

## 7. Nginx reverse proxy

Create Nginx server block:

```bash
sudo nano /etc/nginx/sites-available/marinapp.conf
```

Paste:

```nginx
server {
  listen 80;
  server_name app.marin.cr;

  client_max_body_size 20m;

  location / {
    proxy_pass         http://127.0.0.1:9095;
    proxy_http_version 1.1;

    proxy_set_header   Host $host;
    proxy_set_header   X-Real-IP $remote_addr;
    proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header   X-Forwarded-Proto $scheme;
    proxy_set_header   Upgrade $http_upgrade;
    proxy_set_header   Connection keep-alive;

    proxy_read_timeout 300;
  }
}
```

Enable and reload:

```bash
sudo ln -sf /etc/nginx/sites-available/marinapp.conf /etc/nginx/sites-enabled/marinapp.conf
sudo nginx -t
sudo systemctl reload nginx
```

Allow firewall ports:

```bash
sudo ufw allow 80,443/tcp
```

---

## 8. Let’s Encrypt TLS certificate

Run certbot (you already have it installed):

```bash
sudo certbot --nginx -d app.marin.cr -m oscar@marin.cr --agree-tos --no-eff-email
```

This will:

* Request a cert for `app.marin.cr`
* Edit your Nginx config to add SSL
* Reload Nginx

---

## 9. Auto-renewal check

Certbot installs a systemd timer that runs twice daily.

Check it:

```bash
systemctl status certbot.timer
```

Test renewal:

```bash
sudo certbot renew --dry-run
```

---

## 10. Verify HTTPS

```bash
curl -I https://app.marin.cr/
```

You should see:

```
HTTP/1.1 200 OK
server: nginx
```

---

## 11. Updating the app

To deploy a new version:

```bash
cd /root/marinapp/Marin3
git pull
cd /root/marinapp
docker compose up -d --build
sudo nginx -t && sudo systemctl reload nginx
```

---

## 🔑 Key Points

* Keep **/root/marinapp/.dpkeys** — this ensures login cookies persist across restarts.
* Don’t use `docker compose down -v` in production (it deletes volumes). Use `restart`, `up -d`, or `stop/start`.
* DNS for `app.marin.cr` must always point to your VPS’s IP.
* Let’s Encrypt certs auto-renew; your email `oscar@marin.cr` gets expiry notices.

---

✅ Following these steps, you’ll have a fully containerized ASP.NET Core app running behind Nginx with HTTPS on **[https://app.marin.cr](https://app.marin.cr)**, ready to redeploy anytime.

---

Do you want me to also include in this KB the **final Nginx config file** after Certbot edits it (with both `listen 80` and `listen 443 ssl` blocks), so you know exactly what to expect in `/etc/nginx/sites-available/marinapp.conf`?
