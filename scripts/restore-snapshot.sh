#!/bin/sh
set -eu
pg_restore --exit-on-error --single-transaction --no-owner --no-acl \
  --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" /snapshot/convertbank.dump
touch /var/lib/postgresql/.snapshot-restored
