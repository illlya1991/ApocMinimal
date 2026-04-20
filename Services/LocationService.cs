using System;
using System.Collections.Generic;
using ApocMinimal.Database;
using ApocMinimal.Models.LocationData;
using ApocMinimal.Models.PersonData;

namespace ApocMinimal.Services
{
    /// <summary>
    /// Сервис для работы с локациями.
    /// Загружает все локации один раз и предоставляет быстрый доступ через индексы.
    /// </summary>
    public class LocationService
    {
        private readonly DatabaseManager _db;

        // Основное хранилище: ID → Location
        private Dictionary<int, Location> _allLocations;

        // Индекс: ParentId → список ChildId
        private Dictionary<int, List<int>> _childrenByParent;

        // Индекс: LocationType → список Id
        private Dictionary<LocationType, List<int>> _locationsByType;

        // Индекс: CommercialType → список Id (для коммерческих локаций)
        private Dictionary<CommercialType, List<int>> _commercialByType;

        // Корневые локации (ParentId = 0)
        private List<int> _rootLocations;

        // Кэш для NPC по локациям (LocationId → список NPC)
        private Dictionary<int, List<Npc>> _npcsByLocation;

        // Флаг инициализации
        private bool _isInitialized = false;

        // Статистика
        public int TotalLocations { get; private set; }
        public int TotalCities { get; private set; }
        public int TotalDistricts { get; private set; }
        public int TotalStreets { get; private set; }
        public int TotalBuildings { get; private set; }
        public int TotalFloors { get; private set; }
        public int TotalApartments { get; private set; }
        public int TotalCommercial { get; private set; }

        public LocationService(DatabaseManager db)
        {
            _db = db;
            _allLocations = new Dictionary<int, Location>();
            _childrenByParent = new Dictionary<int, List<int>>();
            _locationsByType = new Dictionary<LocationType, List<int>>();
            _commercialByType = new Dictionary<CommercialType, List<int>>();
            _rootLocations = new List<int>();
            _npcsByLocation = new Dictionary<int, List<Npc>>();

            // Инициализируем словари для всех типов
            foreach (LocationType type in Enum.GetValues(typeof(LocationType)))
            {
                _locationsByType[type] = new List<int>();
            }
            foreach (CommercialType ctype in Enum.GetValues(typeof(CommercialType)))
            {
                _commercialByType[ctype] = new List<int>();
            }
        }

        /// <summary>
        /// Инициализация сервиса. Загружает все локации из базы данных.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            Console.WriteLine("LocationService: Начало загрузки локаций...");
            DateTime start = DateTime.Now;

            LoadAllLocations();
            BuildIndexes();

            _isInitialized = true;

            DateTime end = DateTime.Now;
            Console.WriteLine($"LocationService: Загружено {TotalLocations} локаций за {(end - start).TotalMilliseconds:F0} мс");
            Console.WriteLine($"  Городов: {TotalCities}");
            Console.WriteLine($"  Районов: {TotalDistricts}");
            Console.WriteLine($"  Улиц: {TotalStreets}");
            Console.WriteLine($"  Зданий: {TotalBuildings}");
            Console.WriteLine($"  Этажей: {TotalFloors}");
            Console.WriteLine($"  Квартир: {TotalApartments}");
            Console.WriteLine($"  Коммерческих: {TotalCommercial}");
        }

        /// <summary>
        /// Загрузка всех локаций из базы данных.
        /// </summary>
        private void LoadAllLocations()
        {
            List<Location> locations = _db.GetAllLocations();
            TotalLocations = locations.Count;

            for (int i = 0; i < locations.Count; i++)
            {
                Location loc = locations[i];
                _allLocations[loc.Id] = loc;
            }
        }

        /// <summary>
        /// Построение индексов для быстрого доступа.
        /// </summary>
        private void BuildIndexes()
        {
            // Сбрасываем счётчики
            TotalCities = 0;
            TotalDistricts = 0;
            TotalStreets = 0;
            TotalBuildings = 0;
            TotalFloors = 0;
            TotalApartments = 0;
            TotalCommercial = 0;

            foreach (var kvp in _allLocations)
            {
                Location loc = kvp.Value;

                // Индекс по родителю
                if (!_childrenByParent.ContainsKey(loc.ParentId))
                {
                    _childrenByParent[loc.ParentId] = new List<int>();
                }
                _childrenByParent[loc.ParentId].Add(loc.Id);

                // Корневые локации
                if (loc.ParentId == 0)
                {
                    _rootLocations.Add(loc.Id);
                }

                // Индекс по типу
                _locationsByType[loc.Type].Add(loc.Id);

                // Индекс по коммерческому типу
                if (loc.Type == LocationType.Commercial)
                {
                    _commercialByType[loc.CommercialType].Add(loc.Id);
                }

                // Подсчёт статистики
                switch (loc.Type)
                {
                    case LocationType.City: TotalCities++; break;
                    case LocationType.District: TotalDistricts++; break;
                    case LocationType.Street: TotalStreets++; break;
                    case LocationType.Building: TotalBuildings++; break;
                    case LocationType.Floor: TotalFloors++; break;
                    case LocationType.Apartment: TotalApartments++; break;
                    case LocationType.Commercial: TotalCommercial++; break;
                }
            }
        }

        /// <summary>
        /// Обновление кэша NPC по локациям.
        /// Вызывается после загрузки или изменения NPC.
        /// </summary>
        public void UpdateNpcCache(List<Npc> allNpcs)
        {
            _npcsByLocation.Clear();

            for (int i = 0; i < allNpcs.Count; i++)
            {
                Npc npc = allNpcs[i];
                if (!npc.IsAlive) continue;

                if (!_npcsByLocation.ContainsKey(npc.LocationId))
                {
                    _npcsByLocation[npc.LocationId] = new List<Npc>();
                }
                _npcsByLocation[npc.LocationId].Add(npc);
            }
        }

        // ============================================================
        // МЕТОДЫ ПОЛУЧЕНИЯ ЛОКАЦИЙ
        // ============================================================

        /// <summary>
        /// Получить локацию по ID.
        /// </summary>
        public Location GetLocation(int id)
        {
            _allLocations.TryGetValue(id, out Location loc);
            return loc;
        }

        /// <summary>
        /// Получить все локации.
        /// </summary>
        public List<Location> GetAllLocations()
        {
            List<Location> result = new List<Location>(_allLocations.Count);
            foreach (var kvp in _allLocations)
            {
                result.Add(kvp.Value);
            }
            return result;
        }

        /// <summary>
        /// Получить корневые локации (ParentId = 0).
        /// </summary>
        public List<Location> GetRootLocations()
        {
            List<Location> result = new List<Location>(_rootLocations.Count);
            for (int i = 0; i < _rootLocations.Count; i++)
            {
                result.Add(_allLocations[_rootLocations[i]]);
            }
            return result;
        }

        /// <summary>
        /// Получить прямых детей локации.
        /// </summary>
        public List<Location> GetChildren(int parentId)
        {
            List<Location> result = new List<Location>();

            if (_childrenByParent.TryGetValue(parentId, out List<int> childIds))
            {
                for (int i = 0; i < childIds.Count; i++)
                {
                    result.Add(_allLocations[childIds[i]]);
                }
            }

            return result;
        }

        /// <summary>
        /// Получить количество прямых детей локации.
        /// </summary>
        public int GetChildrenCount(int parentId)
        {
            if (_childrenByParent.TryGetValue(parentId, out List<int> childIds))
            {
                return childIds.Count;
            }
            return 0;
        }

        /// <summary>
        /// Получить всех потомков локации (рекурсивно).
        /// </summary>
        public List<Location> GetAllDescendants(int parentId)
        {
            List<Location> result = new List<Location>();
            CollectDescendants(parentId, result);
            return result;
        }

        private void CollectDescendants(int parentId, List<Location> result)
        {
            if (_childrenByParent.TryGetValue(parentId, out List<int> childIds))
            {
                for (int i = 0; i < childIds.Count; i++)
                {
                    int childId = childIds[i];
                    result.Add(_allLocations[childId]);
                    CollectDescendants(childId, result);
                }
            }
        }

        /// <summary>
        /// Получить все локации определённого типа.
        /// </summary>
        public List<Location> GetLocationsByType(LocationType type)
        {
            List<int> ids = _locationsByType[type];
            List<Location> result = new List<Location>(ids.Count);

            for (int i = 0; i < ids.Count; i++)
            {
                result.Add(_allLocations[ids[i]]);
            }

            return result;
        }

        /// <summary>
        /// Получить все коммерческие локации определённого типа.
        /// </summary>
        public List<Location> GetCommercialByType(CommercialType ctype)
        {
            List<int> ids = _commercialByType[ctype];
            List<Location> result = new List<Location>(ids.Count);

            for (int i = 0; i < ids.Count; i++)
            {
                result.Add(_allLocations[ids[i]]);
            }

            return result;
        }

        /// <summary>
        /// Получить только исследованные локации.
        /// </summary>
        public List<Location> GetExploredLocations()
        {
            List<Location> result = new List<Location>();

            foreach (var kvp in _allLocations)
            {
                if (kvp.Value.IsExplored)
                {
                    result.Add(kvp.Value);
                }
            }

            return result;
        }

        /// <summary>
        /// Получить исследованные локации определённого типа.
        /// </summary>
        public List<Location> GetExploredLocationsByType(LocationType type)
        {
            List<int> ids = _locationsByType[type];
            List<Location> result = new List<Location>();

            for (int i = 0; i < ids.Count; i++)
            {
                Location loc = _allLocations[ids[i]];
                if (loc.IsExplored)
                {
                    result.Add(loc);
                }
            }

            return result;
        }

        // ============================================================
        // МЕТОДЫ ДЛЯ РАБОТЫ С NPC
        // ============================================================

        /// <summary>
        /// Получить всех NPC в указанной локации.
        /// </summary>
        public List<Npc> GetNpcsAtLocation(int locationId)
        {
            if (_npcsByLocation.TryGetValue(locationId, out List<Npc> npcs))
            {
                return npcs;
            }
            return new List<Npc>();
        }

        /// <summary>
        /// Получить количество NPC в локации.
        /// </summary>
        public int GetNpcCountAtLocation(int locationId)
        {
            if (_npcsByLocation.TryGetValue(locationId, out List<Npc> npcs))
            {
                return npcs.Count;
            }
            return 0;
        }

        /// <summary>
        /// Получить всех NPC в локации и всех её подлокациях.
        /// </summary>
        public List<Npc> GetNpcsInArea(int locationId)
        {
            List<Npc> result = new List<Npc>();
            CollectNpcsInArea(locationId, result);
            return result;
        }

        private void CollectNpcsInArea(int locationId, List<Npc> result)
        {
            // Добавляем NPC в текущей локации
            if (_npcsByLocation.TryGetValue(locationId, out List<Npc> npcs))
            {
                result.AddRange(npcs);
            }

            // Рекурсивно обходим детей
            if (_childrenByParent.TryGetValue(locationId, out List<int> childIds))
            {
                for (int i = 0; i < childIds.Count; i++)
                {
                    CollectNpcsInArea(childIds[i], result);
                }
            }
        }

        /// <summary>
        /// Переместить NPC в другую локацию.
        /// </summary>
        public void MoveNpc(Npc npc, int newLocationId)
        {
            // Удаляем из старой локации
            if (_npcsByLocation.TryGetValue(npc.LocationId, out List<Npc> oldList))
            {
                oldList.Remove(npc);
            }

            // Обновляем у NPC
            npc.LocationId = newLocationId;

            // Добавляем в новую локацию
            if (!_npcsByLocation.ContainsKey(newLocationId))
            {
                _npcsByLocation[newLocationId] = new List<Npc>();
            }
            _npcsByLocation[newLocationId].Add(npc);
        }

        // ============================================================
        // МЕТОДЫ ДЛЯ НАВИГАЦИИ
        // ============================================================

        /// <summary>
        /// Получить полный путь от корня до локации.
        /// </summary>
        public List<Location> GetPathToRoot(int locationId)
        {
            List<Location> path = new List<Location>();

            Location current = GetLocation(locationId);
            while (current != null)
            {
                path.Add(current);
                current = GetLocation(current.ParentId);
            }

            path.Reverse();
            return path;
        }

        /// <summary>
        /// Получить родительскую локацию.
        /// </summary>
        public Location GetParent(int locationId)
        {
            Location loc = GetLocation(locationId);
            if (loc != null && loc.ParentId > 0)
            {
                return GetLocation(loc.ParentId);
            }
            return null;
        }

        /// <summary>
        /// Получить здание, в котором находится локация.
        /// </summary>
        public Location GetBuildingAncestor(int locationId)
        {
            Location current = GetLocation(locationId);

            while (current != null)
            {
                if (current.Type == LocationType.Building || current.Type == LocationType.Commercial)
                {
                    return current;
                }
                current = GetLocation(current.ParentId);
            }

            return null;
        }

        /// <summary>
        /// Получить улицу, на которой находится локация.
        /// </summary>
        public Location GetStreetAncestor(int locationId)
        {
            Location current = GetLocation(locationId);

            while (current != null)
            {
                if (current.Type == LocationType.Street)
                {
                    return current;
                }
                current = GetLocation(current.ParentId);
            }

            return null;
        }

        /// <summary>
        /// Получить район, в котором находится локация.
        /// </summary>
        public Location GetDistrictAncestor(int locationId)
        {
            Location current = GetLocation(locationId);

            while (current != null)
            {
                if (current.Type == LocationType.District)
                {
                    return current;
                }
                current = GetLocation(current.ParentId);
            }

            return null;
        }

        // ============================================================
        // МЕТОДЫ ДЛЯ ПОИСКА
        // ============================================================

        /// <summary>
        /// Найти локации по имени (частичное совпадение).
        /// </summary>
        public List<Location> FindLocationsByName(string searchTerm)
        {
            List<Location> result = new List<Location>();
            string lowerSearch = searchTerm.ToLowerInvariant();

            foreach (var kvp in _allLocations)
            {
                Location loc = kvp.Value;
                if (loc.Name.ToLowerInvariant().Contains(lowerSearch) ||
                    loc.FullName.ToLowerInvariant().Contains(lowerSearch))
                {
                    result.Add(loc);
                }
            }

            return result;
        }

        /// <summary>
        /// Найти ближайшую локацию с ресурсом.
        /// </summary>
        public Location FindNearestLocationWithResource(int startLocationId, string resourceName)
        {
            Location start = GetLocation(startLocationId);
            if (start == null) return null;

            Location street = GetStreetAncestor(startLocationId);
            if (street == null) return null;

            // Ищем на той же улице
            List<Location> streetBuildings = GetChildren(street.Id);
            for (int i = 0; i < streetBuildings.Count; i++)
            {
                Location building = streetBuildings[i];
                if (!building.IsExplored) continue;

                List<Location> descendants = GetAllDescendants(building.Id);
                for (int j = 0; j < descendants.Count; j++)
                {
                    Location loc = descendants[j];
                    if (loc.ResourceNodes.ContainsKey(resourceName) && loc.ResourceNodes[resourceName] > 0)
                    {
                        return loc;
                    }
                }
            }

            return null;
        }

        // ============================================================
        // ОТЛАДКА
        // ============================================================

        /// <summary>
        /// Получить статистику по памяти.
        /// </summary>
        public string GetMemoryStats()
        {
            long locationsMemory = _allLocations.Count * 200; // примерно 200 байт на локацию
            long childrenIndexMemory = 0;

            foreach (var kvp in _childrenByParent)
            {
                childrenIndexMemory += 16 + (kvp.Value.Count * 4); // ключ + список int'ов
            }

            long npcCacheMemory = 0;
            foreach (var kvp in _npcsByLocation)
            {
                npcCacheMemory += 16 + (kvp.Value.Count * 8); // ключ + список ссылок
            }

            long totalMemory = locationsMemory + childrenIndexMemory + npcCacheMemory;

            return $"Локации: {locationsMemory / 1024} КБ, Индексы: {childrenIndexMemory / 1024} КБ, NPC: {npcCacheMemory / 1024} КБ, Всего: {totalMemory / 1024} КБ";
        }

        // ============================================================
        // МЕТОДЫ ОБНОВЛЕНИЯ
        // ============================================================

        /// <summary>
        /// Отметить локацию как исследованную.
        /// </summary>
        public void MarkAsExplored(int locationId)
        {
            Location loc = GetLocation(locationId);
            if (loc != null && !loc.IsExplored)
            {
                loc.IsExplored = true;
                // IsDirty уже установится автоматически через свойство
            }
        }

        /// <summary>
        /// Отметить локацию как зачищенную.
        /// </summary>
        public void MarkAsCleared(int locationId)
        {
            Location loc = GetLocation(locationId);
            if (loc != null && loc.Status != LocationStatus.Cleared)
            {
                loc.Status = LocationStatus.Cleared;
                // IsDirty уже установится автоматически через свойство
            }
        }
        /// <summary>
        /// Обновить ресурсы в локации.
        /// </summary>
        public void UpdateResourceNodes(int locationId, Dictionary<string, double> resourceNodes)
        {
            Location loc = GetLocation(locationId);
            if (loc != null)
            {
                loc.ResourceNodes = resourceNodes;
                // IsDirty уже установится автоматически через свойство
            }
        }
    }
}