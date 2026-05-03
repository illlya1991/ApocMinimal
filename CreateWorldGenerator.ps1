# ============================================================
# Скрипт создания WorldGenerator проекта
# ============================================================

$MainProjectPath = "C:\Users\illlya\Documents\Apoc Minimal\ApocMinimal"
$WorldGeneratorPath = Join-Path $MainProjectPath "WorldGenerator"

# Создание папок
Write-Host "Creating folders..." -ForegroundColor Yellow
@(
    "$WorldGeneratorPath\Generation",
    "$WorldGeneratorPath\Database",
    "$WorldGeneratorPath\Models\LocationData",
    "$WorldGeneratorPath\Models\PersonData\NpcData",
    "$WorldGeneratorPath\Models\StatisticsData",
    "$WorldGeneratorPath\Models\ResourceData",
    "$WorldGeneratorPath\Models\GameActions"
) | ForEach-Object { New-Item -ItemType Directory -Force -Path $_ | Out-Null }

# Копирование файлов из основного проекта
Write-Host "Copying files from main project..." -ForegroundColor Yellow
$filesToCopy = @(
    "Models\LocationData\Location.cs",
    "Models\PersonData\Npc.cs",
    "Models\PersonData\Monster.cs",
    "Models\PersonData\MonsterFaction.cs",
    "Models\PersonData\PlayerFaction.cs",
    "Models\PersonData\FactionCoefficients.cs",
    "Models\PersonData\TrueTerminal.cs",
    "Models\PersonData\NpcData\CharacterTrait.cs",
    "Models\PersonData\NpcData\Emotion.cs",
    "Models\PersonData\NpcData\Injury.cs",
    "Models\PersonData\NpcData\MemoryEntry.cs",
    "Models\PersonData\NpcData\Need.cs",
    "Models\PersonData\NpcData\NpcInventoryItem.cs",
    "Models\StatisticsData\Characteristic.cs",
    "Models\StatisticsData\Condition.cs",
    "Models\StatisticsData\Enums.cs",
    "Models\StatisticsData\Modifier.cs",
    "Models\StatisticsData\Statistics.cs",
    "Models\StatisticsData\StatisticsExtensions.cs",
    "Models\ResourceData\Resource.cs",
    "Models\ResourceData\ResourceCatalogEntry.cs",
    "Models\GameActions\NpcAction.cs",
    "DB_Managers\DatabaseManager.cs",
    "DB_Managers\DatabaseManager.Locations.cs",
    "DB_Managers\DatabaseManager.Npcs.cs",
    "DB_Managers\DatabaseManager.Resources.cs",
    "DB_Managers\DatabaseManager.Quests.cs",
    "DB_Managers\DatabaseManager.Techniques.cs",
    "DB_Managers\DatabaseManager.Player.cs"
)

foreach ($file in $filesToCopy) {
    $src = Join-Path $MainProjectPath $file
    $dest = Join-Path $WorldGeneratorPath $file
    if (Test-Path $src) {
        $destDir = Split-Path $dest -Parent
        New-Item -ItemType Directory -Force -Path $destDir | Out-Null
        Copy-Item -Path $src -Destination $dest -Force
        Write-Host "  Copied: $file"
    }
}

# Создание WorldGenerator.csproj
Write-Host "Creating project files..." -ForegroundColor Yellow
@"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>ApocMinimal</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="System.Data.SQLite.Core" Version="1.0.118" />
  </ItemGroup>
</Project>
"@ | Out-File -FilePath "$WorldGeneratorPath\WorldGenerator.csproj" -Encoding UTF8

# Создание Program.cs
@'
using ApocMinimal.Generation;
using ApocMinimal.Database;

namespace ApocMinimal;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("========================================================");
        Console.WriteLine("     APOCALYPSE MINIMAL - WORLD GENERATOR");
        Console.WriteLine("========================================================");
        Console.WriteLine();

        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string outputDbPath = Path.Combine(baseDir, "generated_world.db");
        string templateDbPath = args.Length > 0 ? args[0] : Path.Combine(baseDir, "DataBase", "apoc_minimal_template.db");

        if (!File.Exists(templateDbPath))
        {
            Console.WriteLine("ERROR: Template file not found: " + templateDbPath);
            Console.WriteLine("Usage: WorldGenerator.exe \"path\\to\\apoc_minimal_template.db\"");
            return;
        }

        var generator = new WorldSimulator();
        
        try
        {
            var result = generator.GenerateWorld(templateDbPath, outputDbPath);
            
            Console.WriteLine();
            Console.WriteLine("========================================================");
            Console.WriteLine("              GENERATION COMPLETED");
            Console.WriteLine("========================================================");
            Console.WriteLine("Saved to: " + outputDbPath);
            Console.WriteLine("Statistics:");
            Console.WriteLine("  Level 2 humans: " + result.Level2Humans.ToString("N0"));
            Console.WriteLine("  Monsters: " + result.Monsters.ToString("N0"));
            Console.WriteLine("  Mortality rate: " + result.MortalityRate.ToString("P1"));
            Console.WriteLine("  Time elapsed: " + result.ElapsedMilliseconds + " ms");
        }
        catch (Exception ex)
        {
            Console.WriteLine("CRITICAL ERROR: " + ex.Message);
            Console.WriteLine(ex.StackTrace);
        }
        
        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}
'@ | Out-File -FilePath "$WorldGeneratorPath\Program.cs" -Encoding UTF8

Write-Host "  Created: Program.cs"
Write-Host "  Created: WorldGenerator.csproj"

Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host "Project created at: $WorldGeneratorPath" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Now run the following commands to build and test:"
Write-Host ""
Write-Host "  cd `"$WorldGeneratorPath`""
Write-Host "  dotnet build"
Write-Host "  dotnet run -- `"$MainProjectPath\DataBase\apoc_minimal_template.db`""
Write-Host ""

Read-Host "Press Enter to exit"