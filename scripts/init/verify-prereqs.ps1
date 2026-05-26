$ErrorActionPreference = "Stop"

function Write-Ok {
  param([string]$Message)
  Write-Host "[OK] $Message" -ForegroundColor Green
}

function Write-Warn {
  param([string]$Message)
  Write-Host "[WARN] $Message" -ForegroundColor Yellow
}

function Assert-Command {
  param(
    [string]$Name,
    [scriptblock]$VersionCommand
  )

  try {
    $result = & $VersionCommand
    Write-Ok "$Name disponivel: $result"
  }
  catch {
    throw "$Name nao encontrado ou indisponivel."
  }
}

Assert-Command -Name "Git" -VersionCommand { git --version }
Assert-Command -Name "Docker" -VersionCommand { docker --version }

$dotnetVersion = dotnet --version
Write-Ok ".NET SDK disponivel: $dotnetVersion"

if ($dotnetVersion -notmatch '^(8|9)\.') {
  Write-Warn "Versao do SDK fora do esperado para o laboratorio. Esperado: 8.x ou superior compativel com net8.0."
}

if (-not (Test-Path "lab-microservices.sln")) {
  throw "Arquivo lab-microservices.sln nao encontrado na raiz do repositorio."
}

Write-Ok "Solution encontrada."

if (-not (Test-Path ".env") -and (Test-Path ".env.example")) {
  Write-Warn "Arquivo .env ainda nao existe. A stack sobe com defaults, mas voce pode copiar .env.example para personalizar."
}

Write-Ok "Validacao basica concluida."
