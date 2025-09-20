#!/bin/bash

echo "====================================="
echo "  Configure MARIN_APP Environment"
echo "====================================="
echo

# Ask for Database Server
read -p "Database server: " MARIN_APP_DB_SERVER

# Ask for Database Name (default marinapp)
read -p "Database name [marinapp]: " MARIN_APP_DB_NAME
MARIN_APP_DB_NAME=${MARIN_APP_DB_NAME:-marinapp}

# Ask for Username (default admin)
read -p "User name [admin]: " MARIN_APP_DB_USER
MARIN_APP_DB_USER=${MARIN_APP_DB_USER:-admin}

# Ask for Password (no default, silent input)
read -s -p "Password: " MARIN_APP_DB_PASSWORD
echo

# Ask for Port (default 5432)
read -p "Port [5432]: " MARIN_APP_DB_PORT
MARIN_APP_DB_PORT=${MARIN_APP_DB_PORT:-5432}

# Export variables for current shell
export MARIN_APP_DB_SERVER="$MARIN_APP_DB_SERVER"
export MARIN_APP_DB_NAME="$MARIN_APP_DB_NAME"
export MARIN_APP_DB_USER="$MARIN_APP_DB_USER"
export MARIN_APP_DB_PASSWORD="$MARIN_APP_DB_PASSWORD"
export MARIN_APP_DB_PORT="$MARIN_APP_DB_PORT"

echo
echo "====================================="
echo "Environment variables have been set:"
echo "  MARIN_APP_DB_SERVER=$MARIN_APP_DB_SERVER"
echo "  MARIN_APP_DB_NAME=$MARIN_APP_DB_NAME"
echo "  MARIN_APP_DB_USER=$MARIN_APP_DB_USER"
echo "  MARIN_APP_DB_PASSWORD=********"
echo "  MARIN_APP_DB_PORT=$MARIN_APP_DB_PORT"
echo "====================================="
