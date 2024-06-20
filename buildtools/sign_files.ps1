<#
.Synopsis
    Authenticode-sign the set of provided files in-place.
.DESCRIPTION
    Authenticode-sign the set of provided files in-place.
    You can pass either a file (or files e.g. -Files file1, file2 - mind the limits of PS command line though)
    or a folder with filter and recurse conditions (e.g. -Path folder -Filters *.dll,*.exe -Recurse).
#>

[CmdletBinding()]
Param
(
    [Parameter(Mandatory=$true, ParameterSetName="Files")]
    [string[]]$Files,

    [Parameter(Mandatory=$true, ParameterSetName="Path")]
    [string]$Path,

    [Parameter(ParameterSetName="Path")]
    [string[]]$Filters,

    [Parameter(ParameterSetName="Path")]
    [switch]$Recurse
)

Begin
{
    $ErrorActionPreference = "Stop"
    $unsignedS3bucket = $Env:UNSIGNED_BUCKET
    $signedS3bucket = $Env:SIGNED_BUCKET

    $FilesToSign = @()

    if ($PSCmdlet.ParameterSetName -eq "Files")
    {
        $FilesToSign = $Files
    }
    else
    {
        if ($Recurse)
        {
            $FilesToSign = Get-ChildItem -Path $Path -Include $Filters -File -Recurse | Select-Object -ExpandProperty FullName
        }
        else 
        {
            $FilesToSign = Get-ChildItem -Path $Path\* -Include $Filters -File | Select-Object -ExpandProperty FullName
        }
    }

    if ($FilesToSign.Count -eq 0) 
    { 
        return "Nothing to sign" 
    }

    filter ValidateJob()
    {
        $job = $_
        if ($job.State -eq "Failed")
        {
            throw $job.JobStateInfo.Reason.ErrorRecord
        }
    }

    $sw = [Diagnostics.Stopwatch]::StartNew()

    $signFile = 
    {
        param($file)

        $key = Split-Path $file -leaf
        $key = "XRayDotNetSignerProfile/AuthenticodeSigner-SHA256-RSA/$key"
        $retryCount = 0
        $maxRetryCount = 10

        Write-Host "Signing File: ", $file
        do {
            $versionId = aws s3api put-object --bucket $unsignedS3bucket --key $key --body $file --query VersionId --acl bucket-owner-full-control
            $retryCount++
        } while ($LASTEXITCODE -ne 0 -and $retryCount -le $maxRetryCount)

        if ($LASTEXITCODE -ne 0)
        {
           throw "Upload failed for: $file Reason: " + $Error[0].Exception.Message
        }

        $retryCount = 0        
        do {
            $jobId = aws s3api get-object-tagging --bucket $unsignedS3bucket --key $key --version-id $versionId --query 'TagSet[?Key==`signer-job-id`].Value | [0]'
            $retryCount++
        } while ($jobId -eq "null" -and $retryCount -le $maxRetryCount)

        if ($jobId -eq "null")
        {
           throw "Exceeded retries to check if the object has finished signing for: $file"
        }

        $retryCount = 0
        do {
            Write-Host "Fetching object from signed bucket: ", $key-$jobId
            aws s3api get-object --bucket $signedS3bucket --key $key-$jobId $file
            $retryCount++
        } while ($LASTEXITCODE -ne 0 -and $retryCount -le $maxRetryCount)
        
        if ($LASTEXITCODE -ne 0)
        {
           throw "Download failed for: $file Reason: " + $Error[0].Exception.Message
        }
    }

    Get-Job | Remove-Job

    Write-Host "Signing", $FilesToSign.Count, "file(s)..."
    
    foreach ($file in $FilesToSign)
    {  
        $null = Invoke-Command -ScriptBlock $signFile -ArgumentList $file
    }

    Get-Job | Wait-Job | ValidateJob

    $sw.Stop()
    $totalSec += $sw.Elapsed.TotalSeconds
    Write-Host "Done. Overall execution time : $totalSec sec." 
}
