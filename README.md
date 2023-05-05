# Minecraft 2
 Simple Minecraft in Unity.

Youtube Zdroje:
I Made Minecraft in 24 Hours
How Do Minecraft Worlds Generate?!
Minecraft terrain generation in a nutshell
How to turn a few Numbers into Worlds (Fractal Perlin Noise)
Make Minecraft in Unity 3D Tutorial - 01 - The First Voxel
THIS is how Minecraft Works 💎⛏️
Why Minecraft is a Technical Feat | Explaining the Engineering Behind an Indie Icon

Ovládání:
Těžení - Levé tlačítko myši 
Stavění - Pravé tlačítko myši 
Pohyb - WSAD
Otáčení - Pohyb myší
Změna pozice v Rychlém Baru - Kolečko myši
Kreativní Inventář - I 
Ukládání hry - F1 nebo ESC (zároveň vypne hru)
Ukončení hry - ESC
Informace na pozadí (FPS, pozice a Help) - F3
Skákání - Mezerník
Sprint - Levý Shift


Informace o hře

maximální velikost je 128 chunků

maximální výška je 128 bloků, výše nelze stavět

Pod 0 se hráč neprokope (nachází se zde vrstva Bedrocku)

Velikost jednoho chunku je 16x16

Textury jsou 16x16 pixelů - napůl MC napůl podle zadání (lze přehazovat v Editoru)

Seed hry se převede na číslo, ale uloží se jako string Jméno světa

Po uložení hry (a ukončení) se hráč vrací do hry znovu přes tlačítko Start Game

Ve hře jsou 4 biomy: Pláně, Les, Poušť a Hory

Sníh (bílé bloky) se spawnuje v horách a nad výškou 78 bloků

Nastavení se ukládá do souboru “settings.cfg” a lze editovat v notepadu

V editoru se hra zapíná ze scény "Main Menu"

Svět obsahujes: šedé - kámen, zelené - tráva, bíle - sníh, černé - bedrock, hnědé - hlína, žluté - pisek kostky. 

Dále obsahuje stromy a kaktusy a jejich náležité kostky.

Všechny aktuálně implementované kostky jsou v Creative invenáři a dají se vzít a stavět z nich (pro zjednodušení čitelnosti kostek mají MC ikonky v inv)

Sníh se těží 1 sekundu, Písek 2, Hlína a Tráva 3, Kámen 5 a Bedrock nelze vytěžit nikdy

Zelený svítivý blok s outlinem představuje místo, kde se položí budoucí blok

Perlin noise je používán z Unity knihovny

Chunky se loadují/unloadují podle pozice hráče a jeho nastavené dohlednosti (a velikosti světa)

Hráč získává kostky, které vytěží do "rychlého inventáře" na spodku obrazovky
