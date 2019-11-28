import os
import math
import shutil
from PIL import Image

CONTENT_PATH = "Content/"
EXTRACTED_PATH = "Extracted/"
FILTERED_PATH = "assets/"
XNB_EXT = ".xnb"
PNG_EXT = ".png"

ALL_RES_FOLDERS = [
	"Animals/",
	"Buildings/",
	"Characters/",
	"Characters/Farmer",
	"Characters/Monsters",
	"LooseSprites/",
	"LooseSprites/Lighting",
	"Maps/",
	"Maps/Mines/",
	"Mines/",
	"Minigames/",
	"Portraits/",
	"TerrainFeatures/",
	"TileSheets/"
]

ALL_RES = [
	"Animals/BabyBlue Chicken",
	"Animals/BabyBrown Chicken",
	"Animals/BabyBrown Cow",
	"Animals/BabyGoat",
	"Animals/BabyPig",
	"Animals/BabyRabbit",
	"Animals/BabySheep",
	"Animals/BabyVoid Chicken",
	"Animals/BabyWhite Chicken",
	"Animals/BabyWhite Cow",
	"Animals/Blue Chicken",
	"Animals/Brown Chicken",
	"Animals/Brown Cow",
	"Animals/cat",
	"Animals/cat1",
	"Animals/cat2",
	"Animals/Dinosaur",
	"Animals/dog",
	"Animals/dog1",
	"Animals/dog2",
	"Animals/Duck",
	"Animals/Goat",
	"Animals/horse",
	"Animals/Pig",
	"Animals/Rabbit",
	"Animals/ShearedSheep",
	"Animals/Sheep",
	"Animals/Void Chicken",
	"Animals/White Chicken",
	"Animals/White Cow",
	"Buildings/Barn",
	"Buildings/Big Barn",
	"Buildings/Big Coop",
	"Buildings/Big Shed",
	"Buildings/Coop",
	"Buildings/Deluxe Barn",
	"Buildings/Deluxe Coop",
	"Buildings/Desert Obelisk",
	"Buildings/Earth Obelisk",
	"Buildings/Fish Pond",
	"Buildings/Gold Clock",
	"Buildings/houses",
	"Buildings/Junimo Hut",
	"Buildings/Log Cabin",
	"Buildings/Mill",
	"Buildings/Plank Cabin",
	"Buildings/Shed",
	"Buildings/Shipping Bin",
	"Buildings/Silo",
	"Buildings/Slime Hutch",
	"Buildings/Stable",
	"Buildings/Stone Cabin",
	"Buildings/Water Obelisk",
	"Buildings/Well",
	"Characters/Abigail",
	"Characters/Alex",
	"Characters/Baby",
	"Characters/Baby_dark",
	"Characters/Bear",
	"Characters/Bouncer",
	"Characters/Caroline",
	"Characters/Clint",
	"Characters/ClothesTherapyCharacters",
	"Characters/Dana",
	"Characters/Demetrius",
	"Characters/Dick",
	"Characters/Dwarf",
	"Characters/Elliott",
	"Characters/Emily",
	"Characters/Evelyn",
	"Characters/farmer_girl_base",
	"Characters/femaleRival",
	"Characters/George",
	"Characters/Governor",
	"Characters/Grandpa",
	"Characters/Gunther",
	"Characters/Gus",
	"Characters/Haley",
	"Characters/Harvey",
	"Characters/Henchman",
	"Characters/Jas",
	"Characters/Jodi",
	"Characters/Junimo",
	"Characters/Kent",
	"Characters/Krobus",
	"Characters/Krobus_Trenchcoat",
	"Characters/Leah",
	"Characters/LeahExFemale",
	"Characters/LeahExMale",
	"Characters/Lewis",
	"Characters/Linus",
	"Characters/maleRival",
	"Characters/Marcello",
	"Characters/Mariner",
	"Characters/Marlon",
	"Characters/Marnie",
	"Characters/Maru",
	"Characters/Maru_Hospital",
	"Characters/Morris",
	"Characters/MrQi",
	"Characters/Pam",
	"Characters/Penny",
	"Characters/Pierre",
	"Characters/Robin",
	"Characters/robot",
	"Characters/Sam",
	"Characters/Sandy",
	"Characters/SeaMonsterKrobus",
	"Characters/Sebastian",
	"Characters/Shane",
	"Characters/Shane_JojaMart",
	"Characters/shirts",
	"Characters/Toddler",
	"Characters/Toddler_dark",
	"Characters/Toddler_girl",
	"Characters/Toddler_girl_dark",
	"Characters/Vincent",
	"Characters/WeddingOutfits",
	"Characters/Willy",
	"Characters/Wizard",
	"Characters/Farmer/accessories",
	"Characters/Farmer/farmer_base",
	"Characters/Farmer/farmer_base_bald",
	"Characters/Farmer/farmer_girl_base",
	"Characters/Farmer/farmer_girl_base_bald",
	"Characters/Farmer/hairstyles",
	"Characters/Farmer/hats",
	"Characters/Farmer/pants",
	"Characters/Farmer/shirts",
	"Characters/Farmer/shoeColors",
	"Characters/Farmer/skinColors",
	"Characters/Monsters/Armored Bug",
	"Characters/Monsters/Bat",
	"Characters/Monsters/Big Slime",
	"Characters/Monsters/Bug",
	"Characters/Monsters/Carbon Ghost",
	"Characters/Monsters/Cat",
	"Characters/Monsters/Crow",
	"Characters/Monsters/Duggy",
	"Characters/Monsters/Dust Spirit",
	"Characters/Monsters/Fireball",
	"Characters/Monsters/Fly",
	"Characters/Monsters/Frog",
	"Characters/Monsters/Frost Bat",
	"Characters/Monsters/Ghost",
	"Characters/Monsters/Green Slime",
	"Characters/Monsters/Grub",
	"Characters/Monsters/Haunted Skull",
	"Characters/Monsters/Iridium Bat",
	"Characters/Monsters/Iridium Crab",
	"Characters/Monsters/Lava Bat",
	"Characters/Monsters/Lava Crab",
	"Characters/Monsters/Metal Head",
	"Characters/Monsters/Mummy",
	"Characters/Monsters/Pepper Rex",
	"Characters/Monsters/Rock Crab",
	"Characters/Monsters/Serpent",
	"Characters/Monsters/Shadow Brute",
	"Characters/Monsters/Shadow Girl",
	"Characters/Monsters/Shadow Shaman",
	"Characters/Monsters/Skeleton Mage",
	"Characters/Monsters/Skeleton",
	"Characters/Monsters/Spiker",
	"Characters/Monsters/Squid Kid",
	"Characters/Monsters/Stone Golem",
	"Characters/Monsters/Wilderness Golem",
	"LooseSprites/Billboard",
	"LooseSprites/birds",
	"LooseSprites/boardGame",
	"LooseSprites/boardGameBorder",
	"LooseSprites/buildingPlacementTiles",
	"LooseSprites/chatBox",
	"LooseSprites/Concessions",
	"LooseSprites/ControllerMaps",
	"LooseSprites/cowPhotos",
	"LooseSprites/cowPhotosWinter",
	"LooseSprites/CraneGame",
	"LooseSprites/CraneGame_international",
	"LooseSprites/Cursors",
	"LooseSprites/Cursors2",
	"LooseSprites/daybg",
	"LooseSprites/dye_bench",
	"LooseSprites/emojis",
	"LooseSprites/EmoteMenu",
	"LooseSprites/Fence1",
	"LooseSprites/Fence2",
	"LooseSprites/Fence3",
	"LooseSprites/Fence5",
	"LooseSprites/font_bold",
	"LooseSprites/font_colored",
	"LooseSprites/Giftbox",
	"LooseSprites/JojaCDForm",
	"LooseSprites/JunimoNote",
	"LooseSprites/LanguageButtons",
	"LooseSprites/letterBG",
	"LooseSprites/logo",
	"LooseSprites/map",
	"LooseSprites/Movies",
	"LooseSprites/nightbg",
	"LooseSprites/PlayerStatusList",
	"LooseSprites/SeaMonster",
	"LooseSprites/shadow",
	"LooseSprites/skillTitles",
	"LooseSprites/stardewPanorama",
	"LooseSprites/swimShadow",
	"LooseSprites/tailoring",
	"LooseSprites/textBox",
	"LooseSprites/yellowLettersLogo",
	"LooseSprites/Lighting/greenLight",
	"LooseSprites/Lighting/indoorWindowLight",
	"LooseSprites/Lighting/lantern",
	"LooseSprites/Lighting/projectorLight",
	"LooseSprites/Lighting/sconceLight",
	"LooseSprites/Lighting/windowLight",
	"Maps/bathhouse_tiles",
	"Maps/bugLandTiles",
	"Maps/CarolineGreenhouseTiles",
	"Maps/CarolineGreenhouseTiles_rainy",
	"Maps/characterSheet",
	"Maps/coopTiles",
	"Maps/darkroom_tiles",
	"Maps/desertTiles",
	"Maps/DesertTiles_Extended",
	"Maps/ElliottHouseTiles",
	"Maps/ElliottSeaTiles",
	"Maps/EmilyDreamscapeTiles",
	"Maps/fall_beach",
	"Maps/fall_monsterGraveTiles",
	"Maps/fall_outdoorsTileSheet",
	"Maps/fall_outdoorsTileSheet2",
	"Maps/fall_town",
	"Maps/farmhouse_tiles",
	"Maps/Festivals",
	"Maps/HarveyBalloonTiles",
	"Maps/JojaRuins_TileSheet",
	"Maps/mermaid_house_tiles",
	"Maps/MovieTheater_TileSheet",
	"Maps/MovieTheaterJoja_TileSheet",
	"Maps/MovieTheaterJoja_TileSheet_international",
	"Maps/MovieTheaterScreen_TileSheet",
	"Maps/night_market_tilesheet_objects",
	"Maps/nightSceneMaru",
	"Maps/nightSceneMaruTrees",
	"Maps/paths",
	"Maps/samshowtiles",
	"Maps/sebastianMountainTiles",
	"Maps/sebastianRideTiles",
	"Maps/SewerTiles",
	"Maps/spring_beach",
	"Maps/spring_monsterGraveTiles",
	"Maps/spring_outdoorsTileSheet",
	"Maps/spring_outdoorsTileSheet2",
	"Maps/spring_town",
	"Maps/springobjects",
	"Maps/stadium_tiles",
	"Maps/submarine_tilesheet",
	"Maps/summer_beach",
	"Maps/summer_monsterGraveTiles",
	"Maps/summer_outdoorsTileSheet",
	"Maps/summer_outdoorsTileSheet2",
	"Maps/summer_town",
	"Maps/TownIndoors",
	"Maps/townInterior",
	"Maps/townInterior_2",
	"Maps/walls_and_floors",
	"Maps/winter_beach",
	"Maps/winter_monsterGraveTiles",
	"Maps/winter_outdoorsTileSheet",
	"Maps/winter_outdoorsTileSheet2",
	"Maps/winter_town",
	"Maps/WitchHutTiles",
	"Maps/witchSwampTiles",
	"Maps/WizardHouseTiles",
	"Maps/Mines/mine",
	"Maps/Mines/mine_dark",
	"Maps/Mines/mine_desert",
	"Maps/Mines/mine_desert_dark",
	"Maps/Mines/mine_dino",
	"Maps/Mines/mine_frost",
	"Maps/Mines/mine_frost_dark",
	"Maps/Mines/mine_lava",
	"Maps/Mines/mine_lava_dark",
	"Maps/Mines/mine_quarryshaft",
	"Maps/Mines/mine_slime",
	"Mines/mine",
	"Mines/mine_dark",
	"Mines/mine_desert",
	"Minigames/Clouds",
	"Minigames/Intro",
	"Minigames/jojacorps",
	"Minigames/MaruComet",
	"Minigames/MineCart",
	"Minigames/TitleButtons",
	"Portraits/Abigail",
	"Portraits/Alex",
	"Portraits/Bear",
	"Portraits/Bouncer",
	"Portraits/Caroline",
	"Portraits/Clint",
	"Portraits/Demetrius",
	"Portraits/Dobson",
	"Portraits/Dwarf",
	"Portraits/Elliott",
	"Portraits/Emily",
	"Portraits/Evelyn",
	"Portraits/George",
	"Portraits/Gil",
	"Portraits/Governor",
	"Portraits/Grandpa",
	"Portraits/Gunther",
	"Portraits/Gus",
	"Portraits/Haley",
	"Portraits/Harvey",
	"Portraits/Henchman",
	"Portraits/Jas",
	"Portraits/Jodi",
	"Portraits/Kent",
	"Portraits/Krobus",
	"Portraits/Leah",
	"Portraits/Lewis",
	"Portraits/Linus",
	"Portraits/Marlon",
	"Portraits/Marnie",
	"Portraits/Maru",
	"Portraits/Maru_Hospital",
	"Portraits/Morris",
	"Portraits/MrQi",
	"Portraits/Pam",
	"Portraits/Penny",
	"Portraits/Pierre",
	"Portraits/Robin",
	"Portraits/Sam",
	"Portraits/Sandy",
	"Portraits/Sebastian",
	"Portraits/Shane",
	"Portraits/Vincent",
	"Portraits/Willy",
	"Portraits/Wizard",
	"TerrainFeatures/BuffsIcons",
	"TerrainFeatures/DiggableWall_basic",
	"TerrainFeatures/DiggableWall_basic_dark",
	"TerrainFeatures/DiggableWall_frost",
	"TerrainFeatures/DiggableWall_frost_dark",
	"TerrainFeatures/DiggableWall_lava",
	"TerrainFeatures/DiggableWall_lava_dark",
	"TerrainFeatures/Flooring",
	"TerrainFeatures/Flooring_winter",
	"TerrainFeatures/grass",
	"TerrainFeatures/hoeDirt",
	"TerrainFeatures/hoeDirtDark",
	"TerrainFeatures/hoeDirtSnow",
	"TerrainFeatures/mushroom_tree",
	"TerrainFeatures/Quartz",
	"TerrainFeatures/Stalagmite",
	"TerrainFeatures/Stalagmite_Frost",
	"TerrainFeatures/Stalagmite_Lava",
	"TerrainFeatures/tre_palm",
	"TerrainFeatures/tree1_fall",
	"TerrainFeatures/tree1_spring",
	"TerrainFeatures/tree1_summer",
	"TerrainFeatures/tree1_winter",
	"TerrainFeatures/tree2_fall",
	"TerrainFeatures/tree2_spring",
	"TerrainFeatures/tree2_summer",
	"TerrainFeatures/tree2_winter",
	"TerrainFeatures/tree3_fall",
	"TerrainFeatures/tree3_spring",
	"TerrainFeatures/tree3_winter",
	"TerrainFeatures/upperCavePlants",
	"TileSheets/animations",
	"TileSheets/BuffsIcons",
	"TileSheets/bushes",
	"TileSheets/Craftables",
	"TileSheets/critters",
	"TileSheets/crops",
	"TileSheets/debris",
	"TileSheets/emotes",
	"TileSheets/Floors",
	"TileSheets/fruitTrees",
	"TileSheets/furniture",
	"TileSheets/Projectiles",
	"TileSheets/rain",
	"TileSheets/SecretNotesImages",
	"TileSheets/tools",
	"TileSheets/weapons"
] 

ALL_FIX_RES = [
    "LooseSprites/Fence1",
    "LooseSprites/Fence2",
    "LooseSprites/Fence3",
    "LooseSprites/Fence5",
	"Maps/bathhouse_tiles",
	"Maps/bugLandTiles",
	"Maps/CarolineGreenhouseTiles",
	"Maps/CarolineGreenhouseTiles_rainy",
	"Maps/coopTiles",
	"Maps/darkroom_tiles",
	"Maps/desertTiles",
	"Maps/DesertTiles_Extended",
	"Maps/ElliottHouseTiles",
	"Maps/ElliottSeaTiles",
	"Maps/EmilyDreamscapeTiles",
	"Maps/fall_beach",
	"Maps/fall_outdoorsTileSheet",
	"Maps/fall_outdoorsTileSheet2",
	"Maps/fall_town",
	"Maps/farmhouse_tiles",
	"Maps/Festivals",
	"Maps/JojaRuins_TileSheet",
	"Maps/MovieTheater_TileSheet",
	"Maps/MovieTheaterJoja_TileSheet",
	"Maps/MovieTheaterJoja_TileSheet_international",
	"Maps/MovieTheaterScreen_TileSheet",
	"Maps/night_market_tilesheet_objects",
	"Maps/samshowtiles",
	"Maps/sebastianMountainTiles",
	"Maps/sebastianRideTiles",
	"Maps/SewerTiles",
	"Maps/spring_beach",
	"Maps/spring_outdoorsTileSheet",
	"Maps/spring_outdoorsTileSheet2",
	"Maps/spring_town",
	"Maps/springobjects",
	"Maps/stadium_tiles",
	"Maps/submarine_tilesheet",
	"Maps/summer_beach",
	"Maps/summer_outdoorsTileSheet",
	"Maps/summer_outdoorsTileSheet2",
	"Maps/summer_town",
	"Maps/TownIndoors",
	"Maps/townInterior",
	"Maps/townInterior_2",
	"Maps/walls_and_floors",
	"Maps/winter_beach",
	"Maps/winter_outdoorsTileSheet",
	"Maps/winter_outdoorsTileSheet2",
	"Maps/winter_town",
	"Maps/WitchHutTiles",
	"Maps/witchSwampTiles",
	"Maps/WizardHouseTiles",
	"Maps/Mines/mine",
	"Maps/Mines/mine_dark",
	"Maps/Mines/mine_desert",
	"Maps/Mines/mine_desert_dark",
	"Maps/Mines/mine_dino",
	"Maps/Mines/mine_frost",
	"Maps/Mines/mine_frost_dark",
	"Maps/Mines/mine_lava",
	"Maps/Mines/mine_lava_dark",
	"Maps/Mines/mine_quarryshaft",
	"Maps/Mines/mine_slime"
]

def xnbNodeShell(input, output):
	return os.popen("\"xnb_node/node\" \"xnb_node/main.js\" extract \"%s\" \"%s\"" % (input, output)).read()

def resizeXBRShell(input, output):
	return os.popen("\"ImageResizer/ImageResizer-r133.exe\" /load \"%s\" /resize auto \"XBR 2x\" /save \"%s\"" % (input, output)).read()
	
def main():
	if not os.path.isdir(CONTENT_PATH):
		print("Could not find Content folder")
		return
	
	# Step 1: Extract the XNBs into PNGs
	print("Extracting XNBs...")
	extractXNBs(CONTENT_PATH, EXTRACTED_PATH)
	
	# Step 2: Filter the extracted PNGs using 2xBR
	print("Filtering Extracted PNGs...")
	filterPNGs(EXTRACTED_PATH, FILTERED_PATH)
	
	# Step 3: Fix tiled edged
	print("Fixing Tiled Edges...")
	fixEdgesPNGs(EXTRACTED_PATH, FILTERED_PATH, FILTERED_PATH)
	
	# Step 4: Remove extracted folder
	print("Removing \"%s\" folder..." % (EXTRACTED_PATH))
	shutil.rmtree(EXTRACTED_PATH)

def extractXNBs(fromPath, toPath):
	for resFolder in ALL_RES_FOLDERS:
		fromFolder = fromPath + resFolder
		toFolder = toPath + resFolder
		
		if not os.path.exists(toFolder):
			os.makedirs(toFolder)
		
		print("> " + resFolder)
		xnbNodeShell(fromFolder, toFolder)
		
def filterPNGs(fromPath, toPath):
	for res in ALL_RES:
		fromFile = fromPath + res + PNG_EXT
		toFile = toPath + res + PNG_EXT
		
		toFilePath = os.path.dirname(toFile)
		if not os.path.exists(toFilePath):
			os.makedirs(toFilePath)
			
		print("> " + res)
		resizeXBRShell(fromFile, toFile)

def fixEdgesPNGs(originalPath, fromPath, toPath):
	for res in ALL_FIX_RES:
		originalFile = originalPath + res + PNG_EXT
		fromFile = fromPath + res + PNG_EXT
		toFile = toPath + res + PNG_EXT
		
		toFilePath = os.path.dirname(toFile)
		if not os.path.exists(toFilePath):
			os.makedirs(toFilePath)
			
		print("> " + res)
		edgeFix(originalFile, fromFile, toFile)
		
ORIGINAL_TILE_SIZE = 16
FILTERED_TILE_SIZE = 32
OPACITY_THRESHOLD = 0.95
def edgeFix(originalInput, filteredInput, output):
	originalImage = Image.open(originalInput)
	filteredImage = Image.open(filteredInput)
	
	width, height = filteredImage.size
	tileCountX = int(math.floor(width / FILTERED_TILE_SIZE))
	tileCountY = int(math.floor(height / FILTERED_TILE_SIZE))
		
	for tileX in range(tileCountX):
		absTileX = tileX * FILTERED_TILE_SIZE
		for tileY in range(tileCountY):
			absTileY = tileY * FILTERED_TILE_SIZE
			
			opaqueCount = 0
			for x in range(FILTERED_TILE_SIZE):
				for y in range(FILTERED_TILE_SIZE):
					r, g, b, a = filteredImage.getpixel((absTileX + x, absTileY + y))
					if a > 0: opaqueCount += 1
					
			opaquePercent = opaqueCount / (FILTERED_TILE_SIZE**2)
			if opaquePercent < OPACITY_THRESHOLD: continue
			
			for xTop in range(FILTERED_TILE_SIZE - 1): replacePixel(originalImage, filteredImage, absTileX + xTop, absTileY)
			for xBot in range(FILTERED_TILE_SIZE - 1): replacePixel(originalImage, filteredImage, absTileX + xBot + 1, absTileY + FILTERED_TILE_SIZE - 1)
			for yLeft in range(FILTERED_TILE_SIZE - 1): replacePixel(originalImage, filteredImage, absTileX, absTileY + yLeft + 1)
			for yRight in range(FILTERED_TILE_SIZE - 1): replacePixel(originalImage, filteredImage, absTileX + FILTERED_TILE_SIZE - 1, absTileY + yRight)
	
	filteredImage.save(output)

FILTERED_SCALE = FILTERED_TILE_SIZE / ORIGINAL_TILE_SIZE
def replacePixel(originalImage, filteredImage, x, y):
	originalPixel = originalImage.getpixel((int(math.floor(x / FILTERED_SCALE)), int(math.floor(y / FILTERED_SCALE))))
	filteredImage.putpixel((x, y), originalPixel)
	
if __name__ == "__main__":
	main()