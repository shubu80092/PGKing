#!/bin/bash

# Configuration - Customize these values if needed
# Note: Based on run.ps1, the target image path was formatted this way:
IMAGE_NAME="ghcr.io/shubu80092/pgking:latest"
CONTAINER_NAME="pgking-app"
HOST_PORT=8000
CONTAINER_PORT=80

echo "=== PGKing Deployment Script for Ubuntu ==="

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    echo "Error: Docker could not be found. Please install Docker and try again."
    exit 1
fi

# Optional: Login to GHCR if required (if the repository is private)
read -p "Do you need to log in to GitHub Container Registry? (y/N): " LOGIN_CHOICE
if [[ "$LOGIN_CHOICE" =~ ^[Yy]$ ]] || [[ "$LOGIN_CHOICE" =~ ^[Yy][Ee][Ss]$ ]]; then
    read -p "Enter GitHub Username (e.g., shubu80092): " GH_USER
    read -s -p "Enter GitHub Personal Access Token (PAT): " GH_TOKEN
    echo ""
    echo $GH_TOKEN | sudo docker login ghcr.io -u $GH_USER --password-stdin
    if [ $? -ne 0 ]; then
        echo "Error: Docker login failed."
        exit 1
    fi
fi

echo "Pulling the latest image ($IMAGE_NAME)..."
sudo docker pull $IMAGE_NAME
if [ $? -ne 0 ]; then
    echo "Error: Failed to pull image. Please check your permissions or image name."
    exit 1
fi

echo "Stopping existing container (if any)..."
sudo docker stop $CONTAINER_NAME >/dev/null 2>&1

echo "Removing existing container (if any)..."
sudo docker rm $CONTAINER_NAME >/dev/null 2>&1

echo "Starting the new container..."
echo "Exposing container port $CONTAINER_PORT to local port $HOST_PORT..."
sudo docker run -d \
    --name $CONTAINER_NAME \
    --restart unless-stopped \
    -p $HOST_PORT:$CONTAINER_PORT \
    $IMAGE_NAME

if [ $? -eq 0 ]; then
    echo "=================================================="
    echo "SUCCESS: Deployment completed!"
    echo "The application should be running on http://localhost:$HOST_PORT"
    echo "To view logs, run: sudo docker logs -f $CONTAINER_NAME"
    echo "=================================================="
else
    echo "Error: Failed to start the container."
fi
