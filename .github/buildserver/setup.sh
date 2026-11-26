#!/bin/sh
# One-time setup on the webserver, run as root from the folder holding these files:
#   sh setup.sh "<public key for the GitHub Actions secret TORCH_PUBLISH_KEY>"
set -eu
pubkey=${1:?usage: setup.sh "<ssh public key>"}
compose_dir=/var/www/docker
web_root=$compose_dir/torchbuild

command -v unzip >/dev/null || dnf install -y unzip

id torchbuild >/dev/null 2>&1 || useradd --system --create-home --shell /bin/sh torchbuild
install -m 755 torch-publish /usr/local/bin/torch-publish
install -d -m 755 -o torchbuild -g torchbuild "$web_root"

install -d -m 700 -o torchbuild -g torchbuild /home/torchbuild/.ssh
printf 'restrict,command="/usr/local/bin/torch-publish" %s\n' "$pubkey" > /home/torchbuild/.ssh/authorized_keys
chown torchbuild:torchbuild /home/torchbuild/.ssh/authorized_keys
chmod 600 /home/torchbuild/.ssh/authorized_keys
restorecon -R /home/torchbuild/.ssh 2>/dev/null || true

conf=$compose_dir/nginx/default.conf
if ! grep -q 'server_name build.torchapi.com;' "$conf"; then
  cp "$conf" "$conf.bak-$(date +%Y%m%d-%H%M%S)"
  printf '\n' >> "$conf"
  cat nginx.conf >> "$conf"
fi

compose=$compose_dir/docker-compose.yml
if ! grep -q './torchbuild:/var/www/torchbuild' "$compose"; then
  cp "$compose" "$compose.bak-$(date +%Y%m%d-%H%M%S)"
  sed -i 's#^      - ./nginx/default.conf:/etc/nginx/conf.d/default.conf$#      - ./torchbuild:/var/www/torchbuild:ro\n&#' "$compose"
  grep -q './torchbuild:/var/www/torchbuild' "$compose" || { echo "could not add the nginx volume; add ./torchbuild:/var/www/torchbuild:ro by hand" >&2; exit 1; }
fi

cd "$compose_dir"
docker compose run --rm --no-deps nginx nginx -t
docker compose up -d --no-deps nginx
echo "done: point build.torchapi.com at this server in Cloudflare once a build has published"
