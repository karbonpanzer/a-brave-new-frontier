# A Brave New Frontier

## Overview
*A Brave New Frontier* is a spiritual successor to Shazbot’s Frontier Uniforms. It expands RimWorld’s with a middle ground armor between flak and power armor which is inspire by variousd Sci-Fi and video game sources. Currently materials related to this mod and items are on hiatus but right now I am working on including other materials such as mod settings, factions, and starting scenario and many more things which will be lateral expansions. 

### Design Goals
- Mid-tier protection between flak and marine.
- Clean, vanilla-style art and silhouettes.
- Following the method of armor differing from vanilla Rimworld styling.
- 
- DLC-aware progression: extras for Royalty, Biotech, and Odyssey.

---

## Implemented Materials

The following items are already implemented and serve as the last of the changes when it comes to items and apparel.

| Category            | Items (variants in parentheses) |
|---------------------|----------------------------------|
| Accessories         | Ammo Webbing; Magazine Webbing; Vanguard Webbing; Shocktrooper Webbing; Infantry Pouches; Support Pouches; Medic Pouches |
| Armor (Body)        | Carapace Armor (Medic, Engineer); Heavy Carapace Armor (Medic, Engineer); Officer Carapace Armor; Vanguard Carapace Armor; Centurion (Officer) |
| Armor (Extremities) | Carapace Greaves; Carapace Gauntlets; Aquila Carapace Gauntlets; Tauran Carapace Gauntlets |
| Helmets             | Carapace Helmet (Faceshield, Medic, Officer, Engineer, Thermal); Heavy Carapace Helmet (Faceshield, Medic, Officer, Engineer, Thermal) |
| Uniforms            | Frontier Uniform (Arid, Temperate, Boreal, Polar); Frontier Officer Uniform (Polar) |
| Hats                | Field Hat (Arid, Temperate, Boreal, Polar); Officer Cap |
| Weapons             | Assault Pistol; Hyper-SMG; Hyperblaster; Chem-Rail Rifle; Retro Rifle; Heavy “Autoloader” Rifle; Super-Shotgun; Trench Spike (+ associated projectiles) |

## Factions

BNF introduces new offworld factions that use the gear and weapons above:

- **Civil Offworlders** (`KP_Offworlder_Civil`):  
  Interstellar traders and diplomats backed by core-world governments and megacorporations.  
  *Spacer tech, non-hostile by default.*  
  - Leader title: Representative  
  - Pawn kinds: Settler, Conscript, Marine, Commander, Trader, Representative  
  - Spawns caravans and peaceful visitors; not permanent enemies.

- **Rough Offworlders** (`KP_Offworlder_Rough`):  
  Corporate-backed paramilitary raiders turned slavers.  
  *Spacer tech, always hostile.*  
  - Leader title: Warlord  
  - Pawn kinds: Scrapper, Fence, Engineer, Scout, Marine, Warlord  
  - Multiple weighted combat groups for raid variety.

Both factions:
- Use the Astropolitan culture and support Ideology/Biotech features (memes and xenotypes) if those DLCs are active.
- Have proper caravan trader kinds and spawn rules.

## Scenario

BNF adds a new custom scenario designed to showcase its military gear from the start.

- **Colonial Franchise** (`KP_Outlanders`):  
  A group of offworld contractors sent by a coreworld organization to found a colonial franchise.
  - **Summary:** Strong start — spacer-level weapons and armor, but limited materials.
  - **Starting Pawns:** 5 (configurable, 8 choices); arrive via drop pods.
  - **Special:** 50% chance of cryptosleep sickness at game start.
  - **Starting Gear:**
    - KP_Assault_Pistol (1)  
    - KP_Super_Shotgun (1)  
    - KP_Trench_Spike (2, Steel, Normal)  
    - KP_Light_Flak_Armor (2, Steel, Normal)  
    - KP_Light_Flak_Helmet (2, Steel, Normal)
  - **Starting Items:**  
    - 400 silver  
    - 80 packaged survival meals  
    - 20 industrial components  
    - 40 industrial medicine  
    - 150 steel (baseline crashlanded materials)

This scenario gives players a strong combat opening with BNF gear, but still makes resource management challenging.

---

## Roadmap (High Level)
Most defs are written; art assets/textures are the main work left. DLC-tied content uses conditional defs and fails gracefully if the DLC isn’t installed.

- **Gasmask** – Base-game toxic protection mask.
- **Riot Kit** – Armour, helmet, belt for law/riot units.
- **Psycher Kit** *(Royalty)* – Armour, headgear, staff for psycasters.
- **Mechinator Kit** *(Biotech)* – Helmet, armour, webbing for mechanitors.
- **Partisan Kit** – Padded uniform, slouch hat, webbing for irregulars.
- **Melee Expansion** – Trench club & trench sword to fill melee gaps.
- **Argonaut Kit** *(Odyssey)* – Vacsuit & helmet for spacer/void environments.

---

## Repository Layout

A clean suggested structure for the mod’s files:

