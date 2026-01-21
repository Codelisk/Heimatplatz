#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Tests an MCP (Model Context Protocol) server
.DESCRIPTION
    Sends JSON-RPC requests to test if an MCP server is working correctly
.PARAMETER Command
    The command to start the MCP server
.PARAMETER Args
    Arguments to pass to the command
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$Command,

    [Parameter(Mandatory=$false)]
    [string[]]$Args = @()
)

$ErrorActionPreference = "Stop"

function Send-MCPRequest {
    param(
        [System.Diagnostics.Process]$Process,
        [hashtable]$Request
    )

    $json = $Request | ConvertTo-Json -Depth 10 -Compress
    Write-Host "📤 Sending: $json" -ForegroundColor Cyan

    $Process.StandardInput.WriteLine($json)
    $Process.StandardInput.Flush()

    # Read response
    $response = $Process.StandardOutput.ReadLine()
    Write-Host "📥 Received: $response" -ForegroundColor Green

    return $response | ConvertFrom-Json
}

Write-Host "🧪 Testing MCP Server: $Command $($Args -join ' ')" -ForegroundColor Yellow
Write-Host ""

try {
    # Start the process
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $Command
    $psi.Arguments = $Args -join ' '
    $psi.UseShellExecute = $false
    $psi.RedirectStandardInput = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.CreateNoWindow = $true

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $psi

    # Capture stderr in background
    $stderrBuilder = New-Object System.Text.StringBuilder
    $process.add_ErrorDataReceived({
        param($sender, $e)
        if ($e.Data) {
            [void]$stderrBuilder.AppendLine($e.Data)
        }
    })

    Write-Host "▶️  Starting process..." -ForegroundColor Yellow
    [void]$process.Start()
    $process.BeginErrorReadLine()

    Start-Sleep -Milliseconds 500

    # Test 1: Initialize
    Write-Host ""
    Write-Host "═══════════════════════════════════════" -ForegroundColor Magenta
    Write-Host "Test 1: Initialize Request" -ForegroundColor Magenta
    Write-Host "═══════════════════════════════════════" -ForegroundColor Magenta

    $initRequest = @{
        jsonrpc = "2.0"
        id = 1
        method = "initialize"
        params = @{
            protocolVersion = "2024-11-05"
            capabilities = @{
                roots = @{
                    listChanged = $true
                }
                sampling = @{}
            }
            clientInfo = @{
                name = "test-client"
                version = "1.0.0"
            }
        }
    }

    $initResponse = Send-MCPRequest -Process $process -Request $initRequest

    if ($initResponse.result) {
        Write-Host "✅ Initialize successful!" -ForegroundColor Green
        Write-Host "   Server: $($initResponse.result.serverInfo.name) v$($initResponse.result.serverInfo.version)" -ForegroundColor Gray
        Write-Host "   Capabilities:" -ForegroundColor Gray
        $initResponse.result.capabilities.PSObject.Properties | ForEach-Object {
            Write-Host "     - $($_.Name)" -ForegroundColor Gray
        }
    } else {
        Write-Host "❌ Initialize failed!" -ForegroundColor Red
        exit 1
    }

    # Test 2: List Tools
    Write-Host ""
    Write-Host "═══════════════════════════════════════" -ForegroundColor Magenta
    Write-Host "Test 2: List Tools" -ForegroundColor Magenta
    Write-Host "═══════════════════════════════════════" -ForegroundColor Magenta

    $toolsRequest = @{
        jsonrpc = "2.0"
        id = 2
        method = "tools/list"
        params = @{}
    }

    $toolsResponse = Send-MCPRequest -Process $process -Request $toolsRequest

    if ($toolsResponse.result) {
        $toolCount = $toolsResponse.result.tools.Count
        Write-Host "✅ Found $toolCount tools:" -ForegroundColor Green
        $toolsResponse.result.tools | ForEach-Object {
            Write-Host "   📦 $($_.name)" -ForegroundColor Cyan
            Write-Host "      $($_.description)" -ForegroundColor Gray
        }
    } else {
        Write-Host "⚠️  No tools found or error" -ForegroundColor Yellow
    }

    # Test 3: List Resources
    Write-Host ""
    Write-Host "═══════════════════════════════════════" -ForegroundColor Magenta
    Write-Host "Test 3: List Resources" -ForegroundColor Magenta
    Write-Host "═══════════════════════════════════════" -ForegroundColor Magenta

    $resourcesRequest = @{
        jsonrpc = "2.0"
        id = 3
        method = "resources/list"
        params = @{}
    }

    $resourcesResponse = Send-MCPRequest -Process $process -Request $resourcesRequest

    if ($resourcesResponse.result) {
        $resourceCount = $resourcesResponse.result.resources.Count
        Write-Host "✅ Found $resourceCount resources:" -ForegroundColor Green
        $resourcesResponse.result.resources | ForEach-Object {
            Write-Host "   📄 $($_.uri)" -ForegroundColor Cyan
            Write-Host "      $($_.name): $($_.description)" -ForegroundColor Gray
        }
    } else {
        Write-Host "⚠️  No resources found or error" -ForegroundColor Yellow
    }

    # Test 4: List Prompts
    Write-Host ""
    Write-Host "═══════════════════════════════════════" -ForegroundColor Magenta
    Write-Host "Test 4: List Prompts" -ForegroundColor Magenta
    Write-Host "═══════════════════════════════════════" -ForegroundColor Magenta

    $promptsRequest = @{
        jsonrpc = "2.0"
        id = 4
        method = "prompts/list"
        params = @{}
    }

    $promptsResponse = Send-MCPRequest -Process $process -Request $promptsRequest

    if ($promptsResponse.result) {
        $promptCount = $promptsResponse.result.prompts.Count
        Write-Host "✅ Found $promptCount prompts:" -ForegroundColor Green
        $promptsResponse.result.prompts | ForEach-Object {
            Write-Host "   💬 $($_.name)" -ForegroundColor Cyan
            Write-Host "      $($_.description)" -ForegroundColor Gray
        }
    } else {
        Write-Host "⚠️  No prompts found or error" -ForegroundColor Yellow
    }

    Write-Host ""
    Write-Host "═══════════════════════════════════════" -ForegroundColor Magenta
    Write-Host "✅ MCP Server Test Complete!" -ForegroundColor Green
    Write-Host "═══════════════════════════════════════" -ForegroundColor Magenta

} catch {
    Write-Host ""
    Write-Host "❌ Error: $_" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor Red
    exit 1
} finally {
    if ($process -and !$process.HasExited) {
        Write-Host ""
        Write-Host "🛑 Stopping process..." -ForegroundColor Yellow
        $process.Kill()
        $process.WaitForExit()
    }

    if ($stderrBuilder.Length -gt 0) {
        Write-Host ""
        Write-Host "⚠️  Stderr output:" -ForegroundColor Yellow
        Write-Host $stderrBuilder.ToString() -ForegroundColor Gray
    }
}
