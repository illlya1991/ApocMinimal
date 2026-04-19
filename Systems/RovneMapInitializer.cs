using ApocMinimal.Models.LocationData;
using System.Collections.Generic;

namespace ApocMinimal.Systems;

public static class RovneMapInitializer
{
    private static int _nextId = 1;

    public static (List<Location> locations, int startingLocationId) GenerateLocations()
    {
        _nextId = 1;
        var all = new List<Location>();

        var city = MakeCity("Ровно");
        all.Add(city);

        // --- District Data ---
        // Каждый кортеж: (НазваниеРайона, МассивУлиц)
        // Данные улицы: (Название, Домов1эт, Домов5эт, Домов10эт, Магазины, Супермаркеты, ТЦ, Рынки, Парикмахерские, СалоныКрасоты, Аптеки, Больницы, Заводы, Гостиницы)
        var districtsData = new (string districtName, (string streetName, int c1, int c5, int c10, int shop, int sup, int mall, int mark, int hair, int beau, int phar, int hosp, int fact, int hotel)[] streets)[]
        {
            ("Центр", new[] {
                ("ул. Соборная", 20, 22, 3, 15, 2, 2, 0, 8, 6, 6, 2, 0, 0),
                ("ул. Киевская", 14, 22, 2, 12, 1, 1, 0, 4, 5, 4, 1, 0, 0),
                ("ул. Грушевского", 18, 4, 0, 3, 0, 0, 0, 2, 1, 1, 0, 0, 0),
            }),
            ("Юбилейный", new[] {
                ("пр. Мира", 5, 14, 6, 8, 2, 1, 0, 3, 3, 2, 1, 0, 0),
                ("ул. Клима Савура", 2, 10, 6, 6, 1, 0, 0, 2, 2, 2, 0, 0, 0),
                ("ул. Степана Бандеры", 8, 18, 4, 7, 2, 1, 0, 3, 2, 2, 0, 0, 0),
            }),
            ("Пивничный", new[] {
                ("ул. Макарова", 7, 18, 2, 6, 1, 1, 0, 2, 2, 1, 0, 0, 0),
                ("ул. Ерошенко", 8, 7, 0, 3, 0, 0, 0, 1, 1, 1, 0, 0, 0),
            }),
            ("Боярка", new[] {
                ("ул. Курчатова", 14, 24, 2, 7, 1, 1, 0, 2, 1, 2, 1, 0, 0),
                ("ул. Виделина", 16, 4, 0, 2, 0, 0, 0, 1, 1, 0, 0, 0, 0),
            }),
            ("Новостройки", new[] {
                ("ул. Князя Владимира", 4, 20, 11, 10, 3, 2, 0, 4, 5, 3, 0, 0, 0),
            }),
            ("Басов Кут", new[] {
                ("ул. Коновальца", 15, 3, 0, 3, 0, 0, 0, 2, 1, 1, 0, 0, 0),
            }),
            ("Тинне", new[] {
                ("ул. Соборная (окраина)", 8, 7, 0, 2, 0, 0, 0, 1, 0, 1, 0, 3, 0),
            }),
            ("Железнодорожный", new[] {
                ("ул. Привокзальная", 4, 7, 1, 8, 1, 0, 1, 3, 2, 2, 0, 0, 2),
                ("ул. Железнодорожная", 12, 8, 0, 5, 0, 0, 0, 2, 1, 1, 1, 0, 0),
                ("ул. Степана Бандеры (часть)", 6, 10, 2, 4, 1, 0, 0, 1, 1, 2, 0, 0, 0),
            }),
        };

        int startingLocationId = -1;

        foreach (var (distName, streets) in districtsData)
        {
            bool isStartingDistrict = distName == "Железнодорожный";
            var district = MakeDistrict(distName, city.Id, isStartingDistrict);
            all.Add(district);

            foreach (var (streetName, c1, c5, c10, shop, sup, mall, mark, hair, beau, phar, hosp, fact, hotel) in streets)
            {
                bool isStartingStreet = isStartingDistrict && streetName == "ул. Привокзальная";
                var street = MakeStreet(streetName, district.Id, isStartingDistrict);
                all.Add(street);

                int buildingCounter = 1;

                // 1-этажные дома (частный сектор)
                for (int i = 0; i < c1; i++)
                {
                    var building = MakeBuilding($"Частный дом №{buildingCounter} ({streetName})", street.Id, isStartingDistrict, 1);
                    all.Add(building);
                    buildingCounter++;
                }

                // 5-этажные дома
                for (int i = 0; i < c5; i++)
                {
                    bool explored = isStartingDistrict;
                    var building = MakeBuilding($"5-эт. дом №{buildingCounter} ({streetName})", street.Id, explored, 5);
                    all.Add(building);
                    if (explored)
                    {
                        for (int fl = 1; fl <= 5; fl++)
                        {
                            var floor = MakeFloor($"Этаж {fl}", building.Id, explored);
                            all.Add(floor);
                        }
                    }
                    buildingCounter++;
                }

                // 10-этажные дома
                for (int i = 0; i < c10; i++)
                {
                    bool isStartingBuilding = isStartingStreet && i == 0;
                    var building = MakeBuilding($"10-эт. дом №{buildingCounter} ({streetName})", street.Id, isStartingBuilding, 10);
                    all.Add(building);

                    for (int fl = 1; fl <= 10; fl++)
                    {
                        bool isStartingFloor = isStartingBuilding && fl == 1;
                        var floor = MakeFloor($"Этаж {fl}", building.Id, isStartingBuilding);
                        all.Add(floor);
                        if (isStartingFloor)
                        {
                            startingLocationId = floor.Id;
                        }
                    }
                    buildingCounter++;
                }

                // Коммерческие локации
                GenerateCommercialLocations(all, street.Id, CommercialType.Shop, shop, streetName, ref isStartingStreet, ref startingLocationId);
                GenerateCommercialLocations(all, street.Id, CommercialType.Supermarket, sup, streetName, ref isStartingStreet, ref startingLocationId);
                GenerateCommercialLocations(all, street.Id, CommercialType.Mall, mall, streetName, ref isStartingStreet, ref startingLocationId);
                GenerateCommercialLocations(all, street.Id, CommercialType.Market, mark, streetName, ref isStartingStreet, ref startingLocationId);
                GenerateCommercialLocations(all, street.Id, CommercialType.Hairdresser, hair, streetName, ref isStartingStreet, ref startingLocationId);
                GenerateCommercialLocations(all, street.Id, CommercialType.BeautySalon, beau, streetName, ref isStartingStreet, ref startingLocationId);
                GenerateCommercialLocations(all, street.Id, CommercialType.Pharmacy, phar, streetName, ref isStartingStreet, ref startingLocationId);
                GenerateCommercialLocations(all, street.Id, CommercialType.Hospital, hosp, streetName, ref isStartingStreet, ref startingLocationId);
                GenerateCommercialLocations(all, street.Id, CommercialType.Factory, fact, streetName, ref isStartingStreet, ref startingLocationId);
                GenerateCommercialLocations(all, street.Id, CommercialType.Hotel, hotel, streetName, ref isStartingStreet, ref startingLocationId);
            }
        }

        if (startingLocationId == -1 && all.Count > 0)
        {
            startingLocationId = all[0].Id;
        }

        return (all, startingLocationId);
    }

    private static void GenerateCommercialLocations(List<Location> all, int parentId, CommercialType type, int count, string streetName, ref bool isStartingStreet, ref int startingLocationId)
    {
        for (int i = 0; i < count; i++)
        {
            string name = type switch
            {
                CommercialType.Shop => $"Магазин #{i + 1} ({streetName})",
                CommercialType.Supermarket => $"Супермаркет #{i + 1} ({streetName})",
                CommercialType.Mall => $"ТЦ #{i + 1} ({streetName})",
                CommercialType.Market => $"Рынок ({streetName})",
                CommercialType.Hairdresser => $"Парикмахерская #{i + 1} ({streetName})",
                CommercialType.BeautySalon => $"Салон красоты #{i + 1} ({streetName})",
                CommercialType.Pharmacy => $"Аптека #{i + 1} ({streetName})",
                CommercialType.Hospital => $"Больница #{i + 1} ({streetName})",
                CommercialType.Factory => $"Завод #{i + 1} ({streetName})",
                CommercialType.Hotel => $"Гостиница #{i + 1} ({streetName})",
                _ => $"Коммерческое здание ({streetName})"
            };

            var commercial = new Location
            {
                Id = _nextId++,
                Name = name,
                Type = LocationType.Commercial,
                ParentId = parentId,
                IsExplored = isStartingStreet,
                MapState = MapState.Current,
                DangerLevel = 15,
                Status = LocationStatus.Dangerous,
                CommercialType = type
            };
            all.Add(commercial);

            if (isStartingStreet && startingLocationId == -1)
            {
                startingLocationId = commercial.Id;
            }
        }
    }

    private static Location MakeCity(string name)
    {
        return new Location
        {
            Id = _nextId++,
            Name = name,
            Type = LocationType.City,
            ParentId = 0,
            IsExplored = true,
            MapState = MapState.Current,
            DangerLevel = 10,
            Status = LocationStatus.Dangerous,
        };
    }

    private static Location MakeDistrict(string name, int parentId, bool explored)
    {
        return new Location
        {
            Id = _nextId++,
            Name = name,
            Type = LocationType.District,
            ParentId = parentId,
            IsExplored = explored,
            MapState = MapState.Current,
            DangerLevel = 15,
            Status = LocationStatus.Dangerous,
        };
    }

    private static Location MakeStreet(string name, int parentId, bool explored)
    {
        return new Location
        {
            Id = _nextId++,
            Name = name,
            Type = LocationType.Street,
            ParentId = parentId,
            IsExplored = explored,
            MapState = MapState.Current,
            DangerLevel = 20,
            Status = LocationStatus.Dangerous,
        };
    }

    private static Location MakeBuilding(string name, int parentId, bool explored, int floors)
    {
        return new Location
        {
            Id = _nextId++,
            Name = name,
            Type = LocationType.Building,
            ParentId = parentId,
            IsExplored = explored,
            MapState = MapState.Current,
            DangerLevel = 25 + (floors * 2),
            Status = LocationStatus.Dangerous,
        };
    }

    private static Location MakeFloor(string name, int parentId, bool explored)
    {
        return new Location
        {
            Id = _nextId++,
            Name = name,
            Type = LocationType.Floor,
            ParentId = parentId,
            IsExplored = explored,
            MapState = MapState.Current,
            DangerLevel = 20,
            Status = LocationStatus.Dangerous,
        };
    }
}