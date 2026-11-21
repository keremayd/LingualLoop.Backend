#!/usr/bin/env bash
# Stelinda.Api'yi Google Cloud Run'a yayımlar: proje `stelinda`, bölge
# `europe-west1` (Neon'un Frankfurt'taki veritabanına yakın).
#
#   scripts/cloud-run-deploy.sh            # yalnız yeni sürümü yayımla
#   scripts/cloud-run-deploy.sh --secrets  # gizli ayarları da yükle/yenile
#
# Gizli ayarlar Secret Manager'da durur ve Cloud Run'a ortam değişkeni ya da
# dosya olarak verilir. `--secrets` bunları Stelinda.Api/appsettings.Local.json
# ve Apple anahtar dosyasından okur; değerler ekrana yazılmaz, imaja da girmez
# (.gcloudignore). KartyAdmin:ApiKey bilerek verilmez: içerik girişi ucu
# yayında kapalı kalır.
set -euo pipefail

PROJECT="${PROJECT:-stelinda}"
REGION="${REGION:-europe-west1}"
SERVICE="${SERVICE:-stelinda-api}"
GCLOUD="${GCLOUD:-gcloud}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
LOCAL_SETTINGS="$ROOT/Stelinda.Api/appsettings.Local.json"

# Yerel ayardan bir değer okur (yol "Bölüm.Anahtar"), boşsa durur.
setting() {
  python3 -I -c '
import json, sys
data = json.load(open(sys.argv[1], encoding="utf-8"))
for part in sys.argv[2].split("."):
    data = data[part]
if not isinstance(data, str) or not data.strip():
    sys.exit("boş ayar: " + sys.argv[2])
sys.stdout.write(data)' "$LOCAL_SETTINGS" "$1"
}

# Gizli ayarı oluşturur ya da yeni sürüm ekler; değer stdin'den gelir.
put_secret() {
  local name="$1"
  if ! "$GCLOUD" secrets describe "$name" --project "$PROJECT" >/dev/null 2>&1; then
    "$GCLOUD" secrets create "$name" --project "$PROJECT" \
      --replication-policy automatic --quiet >/dev/null
  fi
  "$GCLOUD" secrets versions add "$name" --project "$PROJECT" \
    --data-file=- --quiet >/dev/null
  echo "gizli ayar yüklendi: $name"
}

# Gereken Google servisleri (açıksa birkaç saniyede geçer).
"$GCLOUD" services enable run.googleapis.com cloudbuild.googleapis.com \
  artifactregistry.googleapis.com secretmanager.googleapis.com \
  --project "$PROJECT" --quiet

if [[ "${1:-}" == "--secrets" ]]; then
  setting PostgresOptions.StelindaConnectionString | put_secret db-connection
  setting JwtOptions.SecretKey | put_secret jwt-secret
  setting AwsOptions.AccessKey | put_secret aws-access-key
  setting AwsOptions.SecretKey | put_secret aws-secret-key
  put_secret apple-signin-key < "$(setting Apple.PrivateKeyPath)"

  # Cloud Run'ın kimliği (varsayılan Compute hesabı) gizli ayarları okuyabilsin.
  number="$("$GCLOUD" projects describe "$PROJECT" --format='value(projectNumber)')"
  for name in db-connection jwt-secret aws-access-key aws-secret-key apple-signin-key; do
    "$GCLOUD" secrets add-iam-policy-binding "$name" --project "$PROJECT" \
      --member "serviceAccount:${number}-compute@developer.gserviceaccount.com" \
      --role roles/secretmanager.secretAccessor --quiet >/dev/null
  done
  echo "Cloud Run gizli ayarları okuyabilir"

  # Kaynaktan derleme aynı hesapla yapılıyor; yeni projelerde bu hesabın
  # derleme izni yok (PERMISSION_DENIED, serviceusage.services.use).
  "$GCLOUD" projects add-iam-policy-binding "$PROJECT" \
    --member "serviceAccount:${number}-compute@developer.gserviceaccount.com" \
    --role roles/run.builder --condition None --quiet >/dev/null
  echo "derleme hesabına Cloud Run Builder rolü verildi"
fi

apple_team="$(setting Apple.TeamId)"
apple_key_id="$(setting Apple.KeyId)"

cd "$ROOT"
"$GCLOUD" run deploy "$SERVICE" \
  --project "$PROJECT" \
  --region "$REGION" \
  --source . \
  --allow-unauthenticated \
  --min-instances 0 \
  --max-instances 2 \
  --cpu 1 \
  --memory 512Mi \
  --cpu-boost \
  --set-env-vars "ASPNETCORE_ENVIRONMENT=Production,Apple__TeamId=${apple_team},Apple__KeyId=${apple_key_id},Apple__PrivateKeyPath=/secrets/apple/AuthKey.p8" \
  --set-secrets "PostgresOptions__StelindaConnectionString=db-connection:latest,JwtOptions__SecretKey=jwt-secret:latest,AwsOptions__AccessKey=aws-access-key:latest,AwsOptions__SecretKey=aws-secret-key:latest,/secrets/apple/AuthKey.p8=apple-signin-key:latest" \
  --quiet

"$GCLOUD" run services describe "$SERVICE" --project "$PROJECT" --region "$REGION" \
  --format 'value(status.url)'
