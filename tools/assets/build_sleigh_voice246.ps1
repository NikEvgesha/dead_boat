# Local generic Windows voice, not a recording or imitation of a person.
$ErrorActionPreference = 'Stop'
$voiceFolder = Join-Path $PSScriptRoot '../../ArtSource/NewYearBoss/Audio246'
New-Item -ItemType Directory -Force -Path $voiceFolder | Out-Null
$voicePath = [System.IO.Path]::GetFullPath((Join-Path $voiceFolder 'Hi_Source_Irina.wav'))
$sleighVoice = New-Object -ComObject SAPI.SpVoice
$voices = $sleighVoice.GetVoices()
$chosen = $null
for ($voiceIndex = 0; $voiceIndex -lt $voices.Count; $voiceIndex++) {
    if ($voices.Item($voiceIndex).GetDescription() -match 'Irina') { $chosen = $voices.Item($voiceIndex); break }
}
if ($null -eq $chosen) { throw 'Russian Windows voice Irina is unavailable' }
$sleighVoice.Voice = $chosen
$sleighVoice.Rate = 1
$sleighVoice.Volume = 100
$voiceStream = New-Object -ComObject SAPI.SpFileStream
try {
    $voiceStream.Format.Type = 22
    $voiceStream.Open($voicePath, 3, $false)
    $sleighVoice.AudioOutputStream = $voiceStream
    [void]$sleighVoice.Speak('Хи.', 0)
} finally {
    $voiceStream.Close()
}
Write-Output "Generated generic Russian hi syllable: $voicePath"
