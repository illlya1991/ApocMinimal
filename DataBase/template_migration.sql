-- ============================================================
-- Миграция шаблонной БД (apoc_minimal_template.db)
-- Запускать через: sqlite3 apoc_minimal_template.db < template_migration.sql
-- ============================================================

-- 1. Добавить колонку LocationId в Npcs (если отсутствует)
ALTER TABLE Npcs ADD COLUMN LocationId INTEGER NOT NULL DEFAULT 0;

-- 2. Добавить колонку Quality в ResourceCatalog (если отсутствует)
ALTER TABLE ResourceCatalog ADD COLUMN Quality INTEGER NOT NULL DEFAULT 1;

-- 3. Выставить качество ресурсов по их категории/редкости
-- (корректируй значения под свои реальные записи в каталоге)
UPDATE ResourceCatalog SET Quality = 1 WHERE Rarity = 'Common'   OR Rarity IS NULL OR Rarity = '';
UPDATE ResourceCatalog SET Quality = 2 WHERE Rarity = 'Uncommon';
UPDATE ResourceCatalog SET Quality = 3 WHERE Rarity = 'Rare';
UPDATE ResourceCatalog SET Quality = 4 WHERE Rarity = 'Epic';
UPDATE ResourceCatalog SET Quality = 5 WHERE Rarity = 'Legendary';

-- 4. Убедиться что ResourceNodes NULL → '{}' для всех локаций
UPDATE Locations SET ResourceNodes = '{}' WHERE ResourceNodes IS NULL OR ResourceNodes = '';

-- 5. Добавить таблицу магазина ресурсов (если отсутствует)
CREATE TABLE IF NOT EXISTS ResourceShop (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    SaveId      TEXT    NOT NULL,
    ResourceName TEXT   NOT NULL,
    UNIQUE(SaveId, ResourceName)
);

-- 6. Добавить IsExplored = 1 для ключевых стартовых локаций
-- (ближайший дом/здание должно быть открыто с самого начала)
-- Раскомментируй и адаптируй под свои локации:
-- UPDATE Locations SET IsExplored = 1 WHERE Type = 'Apartment' LIMIT 5;
-- UPDATE Locations SET IsExplored = 1 WHERE Name LIKE '%Дом%' AND Type = 'Building';

-- ============================================================
-- ПРИМЕЧАНИЕ: Распределение ResourceNodes по локациям делается
-- автоматически кодом MapInitializer при каждой новой игре.
-- Этот скрипт только обновляет схему шаблона.
-- ============================================================
