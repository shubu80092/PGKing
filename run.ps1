# PowerShell script to build and push Docker image to GitHub Container Registry (GHCR)

# Configuration - Customize these values
$GithubOwner = "shubu80092/PGKing"
$ImageName = "shubu80092/pgking"
$Tag = "latest"
$LoginUsername = "shubu80092"

# Format the GHCR tag (names must be lowercase)
$TargetImage = "ghcr.io/$($GithubOwner.ToLower())/$($ImageName.ToLower()):$Tag"

Write-Host "`n--------------------------------------------------" -ForegroundColor Cyan
Write-Host "Target Image: $TargetImage" -ForegroundColor Cyan
Write-Host "--------------------------------------------------`n" -ForegroundColor Cyan

# Optional login step
$LoginChoice = Read-Host -Prompt "Do you need to log in to ghcr.io? (y/n)"
if ($LoginChoice -eq "y" -or $LoginChoice -eq "yes") {
    $LoginUsername = Read-Host -Prompt "Enter your GitHub Login Username (default: $LoginUsername)"
    if ([string]::IsNullOrWhiteSpace($LoginUsername)) {
        $LoginUsername = "rginbox"
    }

    Write-Host "`nEnsure your GitHub PAT has the 'write:packages' permission." -ForegroundColor Yellow
    $Token = Read-Host -Prompt "Enter your GitHub Personal Access Token (PAT)" -AsSecureString
    $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($Token)
    $PlainToken = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
    
    Write-Host "Logging into ghcr.io..." -ForegroundColor Gray
    $PlainToken | docker login ghcr.io -u $LoginUsername --password-stdin
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to authenticate with ghcr.io."
        exit
    }
}

# 1. Build the Docker Image
Write-Host "`n[1/2] Building Docker image..." -ForegroundColor Green
docker build -t $TargetImage .
if ($LASTEXITCODE -ne 0) {
    Write-Error "Docker build failed."
    exit
}

# 2. Push the Docker Image to GHCR
Write-Host "`n[2/2] Pushing image to GitHub Container Registry..." -ForegroundColor Green
docker push $TargetImage
if ($LASTEXITCODE -ne 0) {
    Write-Error "Docker push failed."
    exit
}

Write-Host "`n--------------------------------------------------" -ForegroundColor Green
Write-Host "SUCCESS: Image successfully pushed to ghcr.io!" -ForegroundColor Green
Write-Host "--------------------------------------------------`n" -ForegroundColor Green