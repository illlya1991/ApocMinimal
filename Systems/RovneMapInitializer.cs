using ApocMinimal.Models.LocationData;

namespace ApocMinimal.Systems;

public static class RovneMapInitializer
{
    private static int _nextId = 1;

    public static (List<Location> locations, int startingLocationId) GenerateLocations()
    {
        _nextId = 1;
        var all = new List<Location>();

        var city = Make("Ровно", LocationType.City, 0, true);
        all.Add(city);

        (string districtName, (string street, int count1, int count5, int count10)[] streets)[] districts =
        {
            ("Центр", new[]
            {
                ("ул. Соборная", 20, 22, 3),
                ("ул. Киевская", 14, 22, 2),
                ("ул. Грушевского", 18, 4, 0),
            }),
            ("Юбилейный", new[]
            {
                ("пр. Мира", 5, 14, 6),
                ("ул. Клима Савура", 2, 10, 6),
                ("ул. Степана Бандеры", 8, 18, 4),
            }),
            ("Пивничный", new[]
            {
                ("ул. Макарова", 7, 18, 2),
                ("ул. Ерошенко", 8, 7, 0),
            }),
            ("Боярка", new[]
            {
                ("ул. Курчатова", 14, 24, 2),
                ("ул. Виделина", 16, 4, 0),
            }),
            ("Новостройки", new[]
            {
                ("ул. Князя Владимира", 4, 20, 11),
            }),
            ("Басов Кут", new[]
            {
                ("ул. Коновальца", 15, 3, 0),
            }),
            ("Тинне", new[]
            {
                ("ул. Соборная (окраина)", 8, 7, 0),
            }),
            ("Железнодорожный", new[]
            {
                ("ул. Привокзальная", 4, 7, 1),
                ("ул. Железнодорожная", 12, 8, 0),
                ("ул. Степана Бандеры (часть)", 6, 10, 2),
            }),
        };

        int startingLocationId = -1;
        bool isStartingDistrict = false;

        foreach (var (distName, streets) in districts)
        {
            isStartingDistrict = distName == "Железнодорожный";
            var dist = Make(distName, LocationType.District, city.Id, isStartingDistrict);
            all.Add(dist);

            foreach (var (streetName, c1, c5, c10) in streets)
            {
                bool isStartingStreet = isStartingDistrict && streetName == "ул. Привокзальная";
                var street = Make(streetName, LocationType.Street, dist.Id, isStartingDistrict);
                all.Add(street);

                for (int i = 1; i <= c1; i++)
                {
                    var bld = Make($"Дом №{i} ({streetName})", LocationType.Building, street.Id, false);
                    all.Add(bld);
                }

                int buildingNum = c1 + 1;
                for (int i = 0; i < c5; i++, buildingNum++)
                {
                    bool explored = isStartingDistrict;
                    var bld = Make($"5-эт. дом №{buildingNum} ({streetName})", LocationType.Building, street.Id, explored);
                    all.Add(bld);
                    if (isStartingDistrict)
                    {
                        for (int fl = 1; fl <= 5; fl++)
                        {
                            var floor = Make($"Этаж {fl}", LocationType.Floor, bld.Id, explored);
                            all.Add(floor);
                        }
                    }
                }

                for (int i = 0; i < c10; i++, buildingNum++)
                {
                    bool isStartingBuilding = isStartingStreet && i == 0;
                    var bld = Make($"10-эт. дом №{buildingNum} ({streetName})", LocationType.Building, street.Id, isStartingBuilding);
                    all.Add(bld);

                    for (int fl = 1; fl <= 10; fl++)
                    {
                        bool isStartingFloor = isStartingBuilding && fl == 1;
                        var floor = Make($"Этаж {fl}", LocationType.Floor, bld.Id, isStartingBuilding);
                        all.Add(floor);
                        if (isStartingFloor)
                            startingLocationId = floor.Id;
                    }
                }
            }
        }

        return (all, startingLocationId > 0 ? startingLocationId : 1);
    }

    private static Location Make(string name, LocationType type, int parentId, bool explored) => new()
    {
        Id = _nextId++,
        Name = name,
        Type = type,
        ParentId = parentId,
        IsExplored = explored,
        MapState = MapState.Current,
        DangerLevel = type is LocationType.Building or LocationType.Floor ? 40 : 20,
        Status = LocationStatus.Dangerous,
    };
}
