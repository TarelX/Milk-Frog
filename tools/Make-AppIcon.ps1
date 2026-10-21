param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$directory=Join-Path $ProjectRoot 'src\MilkFrog.App\Assets'
New-Item -ItemType Directory -Force -Path $directory | Out-Null
$image=[System.Drawing.Bitmap]::FromFile((Join-Path $ProjectRoot 'assets\generated\young\animations\frames\idle\00.png'))
$sources=@()
try {
  foreach($size in @(256,64,32,16)) {
    $bitmap=[System.Drawing.Bitmap]::new($size,$size,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics=[System.Drawing.Graphics]::FromImage($bitmap)
    try {
      $graphics.SmoothingMode=[System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
      $graphics.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
      $graphics.Clear([System.Drawing.Color]::Transparent)
      $inset=[Math]::Max(1,[int]($size*.055))
      $graphics.FillEllipse([System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255,255,246,205)),$inset,$inset,$size-$inset*2,$size-$inset*2)
      $margin=[int]($size*.10)
      $graphics.DrawImage($image,[System.Drawing.Rectangle]::new($margin,$margin,$size-$margin*2,$size-$margin*2),[System.Drawing.Rectangle]::new(58,45,396,405),[System.Drawing.GraphicsUnit]::Pixel)
      $memory=[System.IO.MemoryStream]::new(); $bitmap.Save($memory,[System.Drawing.Imaging.ImageFormat]::Png); $sources+=,$memory.ToArray(); $memory.Dispose()
      if($size -eq 256){$bitmap.Save((Join-Path $directory 'app-icon.png'),[System.Drawing.Imaging.ImageFormat]::Png)}
    } finally { $graphics.Dispose(); $bitmap.Dispose() }
  }
} finally { $image.Dispose() }
$stream=[System.IO.File]::Create((Join-Path $directory 'app.ico'))
$writer=[System.IO.BinaryWriter]::new($stream)
try {
  $writer.Write([uint16]0);$writer.Write([uint16]1);$writer.Write([uint16]$sources.Count)
  $offset=6+16*$sources.Count
  for($i=0;$i -lt $sources.Count;$i++){$size=@(256,64,32,16)[$i];$writer.Write([byte]$(if($size -eq 256){0}else{$size}));$writer.Write([byte]$(if($size -eq 256){0}else{$size}));$writer.Write([byte]0);$writer.Write([byte]0);$writer.Write([uint16]1);$writer.Write([uint16]32);$writer.Write([uint32]$sources[$i].Length);$writer.Write([uint32]$offset);$offset+=$sources[$i].Length}
  foreach($data in $sources){$writer.Write($data)}
} finally {$writer.Dispose();$stream.Dispose()}
