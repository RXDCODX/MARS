$files = @(
  'tests/MARS.Commands.Tests/Commands/AggregatePlatformCommand.cs',
  'tests/MARS.Commands.Tests/Commands/BoolTypedCommandProbe.cs',
  'tests/MARS.Commands.Tests/Adapters/PlatformCommandAdaptersTests.cs',
  'tests/MARS.Commands.Tests/Adapters/ApiCommandServiceExecutionTests.cs',
  'tests/MARS.Commands.Tests/Controllers/CommandsControllerTests.cs'
)

foreach ($file in $files) {
  $path = Resolve-Path $file
  $text = Get-Content $path -Raw
  
  # Удалить alias using Platform = ...
  $text = $text -replace "using Platform = MARS\.Commands\.Services\.Entitys\.Platform;\r?\n", ""
  
  # Заменить Platform как тип на Entitys.Platform
  # Pattern: Platform seguido de espacio + identificador (tipo de parámetro/variable)
  $text = $text -replace '\bPlatform\s+([a-z_]\w*)', 'Entitys.Platform $1'
  # Pattern: Platform. (static member access)
  $text = $text -replace '\bPlatform\.', 'Entitys.Platform.'
  # Pattern: Platform) or Platform, or Platform; or Platform= or Platform>
  $text = $text -replace '\bPlatform\)', 'Entitys.Platform)'
  $text = $text -replace '\bPlatform,', 'Entitys.Platform,'
  $text = $text -replace '\bPlatform;', 'Entitys.Platform;'
  $text = $text -replace '\bPlatform=', 'Entitys.Platform='
  $text = $text -replace '\bPlatform>', 'Entitys.Platform>'
  
  [System.IO.File]::WriteAllLines($path, $text, [System.Text.UTF8Encoding]::new($false))
  "Обновлён: $file"
}
