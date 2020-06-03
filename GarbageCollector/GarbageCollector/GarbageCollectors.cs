using NetTopologySuite.Geometries;
using OSMLSGlobalLibrary;
using OSMLSGlobalLibrary.Map;
using OSMLSGlobalLibrary.Modules;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using NetTopologySuite.Mathematics;

namespace GarbageCollector
{
    public class GarbageCollectors : OSMLSModule
    {
        static int xspeed = 1;//Ускорение работы
        int time = 0;//Время действия
        // Тестовая точка.
        double[,] coordGarbageFactory = {{ 48.518825, 44.606714 } };//Координата фабрики по переработке
        double[,] coordGarbageDump = { { 48.518825, 44.606714 } };//Координаты свалки
        double[,] coordStartContainers = { { 48.525212, 44.575567 } }; //Координаты начала контейнеров

        int countContainers = 40; //Количество контейнеров
        double distanceContainers = 0.004; //Дистанция между контейнерами
        double volumeMaxContainers = 300; //Максимальный объем контейнера
        int countGreaterMaxContainers=0; //Превышение объекма контейнера
        int countGreaterMaxContainersNow; //Текущее превышение объема контейнера

        int countUrn = 15; //Количество урн
        double distanceUrn = 0.001; //Дистанция между урнами
        double volumeMaxUrn = 20; //Максимальный объем урны
        int countGreaterMaxUrn=0; //Превышение объема урны
        int countGreaterMaxUrnNow; //Текущее превышение объема урны

        double recycleDumpi = 1;//Число перерабатываемых отходов при гниении
        double speedDumpi = 1000;//Обратная скорость переработки

        double recycleFactoryi = 10;//Число перерабатываемых отходов фабрикой
        double speedFactoryi = 300;//Обратная скорость переработки

        GarbageFactory factoryi; //Фабрика по переработке
        GarbageDump dumpi; //Свалка
        List<GarbageСontainer> containers = new List<GarbageСontainer>(); //Контейнеры
        List<GarbageUrn> urns = new List<GarbageUrn>(); //Урны

        double speedTruck = 20; //Скорость перемещения мусоровоза
        int countTruck = 10;//Число мусоровозов
        double volumeMaxTruck = 1000;//Максимальный объем перевозимый мусоровозом
        double speedJanitor = 2;//Скорость перемещения уборщков (дворника)
        int countJanitor = 3;//Число уборщиков
        double volumeMaxJanitor = 20; //Объем переносимый уборщиком
        List<GarbageTruck> truck = new List<GarbageTruck>();//Мусоровозы
        List<Janitor> janitor = new List<Janitor>();//Уборщики

        double newGarbageCount = 0.1;//Число появляющегося мусора в урнах
        double newGarbageSpeed = 10;//Обратная скорость появления мусора

        protected override void Initialize()
        {
            Console.WriteLine("Начало\n");

            Console.Write("Количество убирающих должно быть меньше или равно количеству убираемых.\n");
            Console.Write($"Введите количество мусоровозов ({countTruck}): ");
            string str = Console.ReadLine();
            int numstr;
            if(Int32.TryParse(str, out numstr))
            {
                countTruck = numstr;
            }
            Console.Write($"Введите количество контейнеров ({countContainers}): ");
            str = Console.ReadLine();
            if (Int32.TryParse(str, out numstr))
            {
                countContainers = numstr;
            }
            Console.Write($"Введите количество уборщиков на однин контейнер ({countJanitor}): ");
            str = Console.ReadLine();
            if (Int32.TryParse(str, out numstr))
            {
                countJanitor = numstr;
            }
            Console.Write($"Введите количество урн на однин контейнер ({countUrn}): ");
            str = Console.ReadLine();
            if (Int32.TryParse(str, out numstr))
            {
                countUrn = numstr;
            }

            //Выводим фабрику и свалку
            dumpi = new GarbageDump(coordGarbageDump[0, 0], coordGarbageDump[0, 1]);
            MapObjects.Add(dumpi);
            factoryi = new GarbageFactory(coordGarbageFactory[0, 0], coordGarbageFactory[0, 1]);
            MapObjects.Add(factoryi);
            

            //Выводим контейнеры
            SelectAndShowRandomСontainer(countContainers, distanceContainers, coordStartContainers[0,0], coordStartContainers[0,1], volumeMaxContainers);
            
            //Выводим урны для каждого контейнера
            for (int i=0; i< containers.Count; i++)
            {
                SelectAndShowRandomUrns(countUrn, distanceUrn, containers[i].X, containers[i].Y, i, volumeMaxUrn);
            }

            //Выводим мусоровозы
            for (int i=0; i<countTruck; i++)
            {
                truck.Add(new GarbageTruck(
                    new Coordinate(MathExtensions.LatLonToSpherMerc(coordGarbageDump[0, 0], coordGarbageDump[0, 1]+0.0001*i)),
                    new Coordinate(MathExtensions.LatLonToSpherMerc(coordGarbageDump[0, 0], coordGarbageDump[0, 1])),
                    volumeMaxTruck));
                MapObjects.Add(truck.Last());
            }
            //Назначаем мусоровозы на контейнеры
            for (int i = 0; i < containers.Count; i++)
            {
                truck[i%countTruck].indexСontainers.Add(i);
            }

            //Выводим дворников
            for (int k = 0; k < containers.Count; k++)//Выбираем дворников для каждого контейнера
            {
                int countJanitorNow = janitor.Count;
                for (int i = countJanitorNow; i < countJanitorNow+countJanitor; i++)
                {
                    janitor.Add(new Janitor(
                        new Coordinate(MathExtensions.LatLonToSpherMerc(containers[k].X, containers[k].Y + 0.00001 * i)),
                        new Coordinate(MathExtensions.LatLonToSpherMerc(containers[k].X, containers[k].Y)),
                        k, volumeMaxJanitor));
                    MapObjects.Add(janitor.Last());
                }
                //Назначаем дворников на урны
                int c = 0;
                for (int i = 0; i < urns.Count; i++)
                {
                    if (urns[i].indexСontainer == k)
                    {
                        janitor[countJanitorNow+c % countJanitor].indexUrns.Add(i);
                        c++;
                    }
                }
            }


        }

        void SelectAndShowRandomUrns(double num, double distance, double coord1, double coord2, int index, double volumeMaxUrn)
        {
            double countUrl = urns.Count + num;
            double radius = Math.Sqrt(2.0) * (distance * (Math.Sqrt(num) - 0.8)) / 2;
            Random rand = new Random();
            for (double i = -radius; i < radius && urns.Count < countUrl; i = i + distance)
            {
                for (double j = -radius; j < radius && urns.Count < countUrl; j = j + distance)
                {
                    double rand1 = (rand.NextDouble() * 2 - 1) * (distance / 3);
                    double rand2 = (rand.NextDouble() * 2 - 1) * (distance / 3);
                    urns.Add(new GarbageUrn(coord1 + i + rand1, coord2 + j + rand2, index, volumeMaxUrn));
                    MapObjects.Add(urns.Last());
                }
            }
        }
        void SelectAndShowRandomСontainer(double num, double distance, double coord1, double coord2, double volumeMaxContainers)
        {
            double countContainer = containers.Count + num;
            double radius = Math.Sqrt(2.0) * (distance * (Math.Sqrt(num) - 0.8)) / 2;
            Random rand = new Random();
            for (double i = -radius; i < radius && containers.Count < countContainer; i = i + distance)
            {
                for (double j = -radius; j < radius && containers.Count < countContainer; j = j + distance)
                {
                    double rand1 = (rand.NextDouble() * 2 - 1) * (distance / 3);
                    double rand2 = (rand.NextDouble() * 2 - 1) * (distance / 3);
                    containers.Add(new GarbageСontainer(coord1 + i + rand1, coord2 + j + rand2, volumeMaxContainers));
                    MapObjects.Add(containers.Last());
                }
            }
        }

        /// <summary>
        /// Вызывается постоянно, здесь можно реализовывать логику перемещений и всего остального, требующего времени.
        /// </summary>
        /// <param name="elapsedMilliseconds">TimeNow.ElapsedMilliseconds</param>
        public override void Update(long elapsedMilliseconds)
        {
            for(int u=0; u<xspeed; u++)
            {
                time++;
                double valueMaxUrn = 0;
                double valueMaxContainer = 0;
                countGreaterMaxUrnNow = 0;
                countGreaterMaxContainersNow = 0;
                countGreaterMaxContainers = 0;
                countGreaterMaxContainersNow = 0;

                if (time % newGarbageSpeed == 0)
                {
                    for (int i = 0; i < urns.Count; i++)//Для всех урн увеличисть число мусора
                    {
                        urns[i].put(newGarbageCount);
                    }
                }
                for (int i = 0; i < janitor.Count; i++)//Для всех уборщиков
                {
                    if (janitor[i].index != janitor[i].indexUrns.Count)//Если не все урны пройдены
                    {
                        if (janitor[i].volumeNow == janitor[i].volumeMax)//Если уборщик заполнен
                        {
                            if (janitor[i].Coordinate.Equals(janitor[i].сoordinateСontainer))//Если достигли контейнера
                            {
                                containers[janitor[i].idСontainer].put(janitor[i].volumeNow);//Выгрузить в контейнер
                                janitor[i].volumeNow = 0;
                            }
                            else
                            {
                                janitor[i].move(janitor[i].сoordinateСontainer, speedJanitor);//Двигаться в сторону контейнера
                            }
                        }
                        else
                        {
                            int indexUrn = janitor[i].indexUrns[janitor[i].index];
                            if (janitor[i].Coordinate.Equals(urns[indexUrn].Coordinate))//Ecли урна достигнута
                            {
                                if (janitor[i].volumeMax - janitor[i].volumeNow < urns[indexUrn].volumeNow)//Если в урне больше мусора чем можно унести
                                {
                                    urns[indexUrn].output(janitor[i].volumeMax - janitor[i].volumeNow);//Собираем из урны возможное
                                    janitor[i].volumeNow = janitor[i].volumeMax;
                                }
                                else
                                {
                                    janitor[i].put(urns[indexUrn].volumeNow);//Собираем из урны все
                                    urns[indexUrn].volumeNow = 0;
                                    janitor[i].index++;//Перейти к следующей урне
                                }
                            }
                            else
                            {
                                janitor[i].move(urns[indexUrn].Coordinate, speedJanitor);
                            }
                        }
                    }
                    else
                    {
                        if (janitor[i].volumeNow != 0)
                        {
                            if (janitor[i].Coordinate.Equals(janitor[i].сoordinateСontainer))//Если достигли контейнера
                            {
                                containers[janitor[i].idСontainer].put(janitor[i].volumeNow);//Выгрузить в контейнер
                                janitor[i].volumeNow = 0;
                                janitor[i].index = 0;
                            }
                            else
                            {
                                janitor[i].move(janitor[i].сoordinateСontainer, speedJanitor);//Двигаться в сторону контейнера
                            }
                        }
                        else
                        {
                            janitor[i].index = 0;
                        }

                    }

                }
                for (int i = 0; i < truck.Count; i++)//Для всех мусоровозов
                {
                    if (truck[i].index != truck[i].indexСontainers.Count)//Если не все контейнеры пройдены
                    {
                        if (truck[i].volumeNow == truck[i].volumeMax)//Если уборщик заполнен
                        {
                            if (truck[i].Coordinate.Equals(truck[i].сoordinateDump))//Если достигли контейнера
                            {
                                dumpi.put(truck[i].volumeNow);//Выгрузить в контейнер
                                truck[i].volumeNow = 0;
                            }
                            else
                            {
                                truck[i].move(dumpi.Coordinate, speedTruck);//Двигаться в сторону контейнера
                            }
                        }
                        else
                        {
                            int indexContainer = truck[i].indexСontainers[truck[i].index];
                            if (truck[i].Coordinate.Equals(containers[indexContainer].Coordinate))//Ecли урна достигнута
                            {
                                if (truck[i].volumeMax - truck[i].volumeNow < containers[indexContainer].volumeNow)//Если в урне больше мусора чем можно унести
                                {
                                    containers[indexContainer].output(truck[i].volumeMax - truck[i].volumeNow);//Собираем из урны возможное
                                    truck[i].volumeNow = truck[i].volumeMax;
                                }
                                else
                                {
                                    truck[i].put(urns[indexContainer].volumeNow);//Собираем из урны все
                                    containers[indexContainer].volumeNow = 0;
                                    truck[i].index++;//Перейти к следующей урне
                                }
                            }
                            else
                            {
                                truck[i].move(containers[indexContainer].Coordinate, speedTruck);
                            }
                        }
                    }
                    else
                    {
                        if (truck[i].volumeNow != 0)
                        {
                            if (truck[i].Coordinate.Equals(truck[i].сoordinateDump))//Если достигли контейнера
                            {
                                dumpi.put(truck[i].volumeNow);//Выгрузить в контейнер
                                truck[i].volumeNow = 0;
                                truck[i].index = 0;
                            }
                            else
                            {
                                truck[i].move(truck[i].сoordinateDump, speedJanitor);//Двигаться в сторону контейнера
                            }
                        }
                        else
                        {
                            truck[i].index = 0;
                        }

                    }
                }
                if (dumpi.volumeNow != 0 && time % speedDumpi == 0)//Уменьшение мусора на свалке из-за гниения
                {
                    if(dumpi.volumeNow< recycleDumpi)
                    {
                        dumpi.volumeNow = 0;
                    }
                    else
                    {
                        dumpi.output(recycleDumpi);
                    }
                }
                if (dumpi.volumeNow != 0 && time % speedFactoryi == 0)//Уменьшение мусора на свалке из-за переработки заводом
                {
                    if (dumpi.volumeNow < recycleFactoryi)
                    {
                        dumpi.volumeNow = 0;
                    }
                    else
                    {
                        dumpi.output(recycleFactoryi);
                    }
                }

                //Вывод данных
                if (time % 20 == 0)
                {
                    for (int i = 0; i < urns.Count; i++)
                    {
                        if (urns[i].volumeNow > urns[i].volumeMax) countGreaterMaxUrnNow++;
                        if (urns[i].volumeNow > valueMaxUrn)
                        {
                            valueMaxUrn = urns[i].volumeNow;
                        }
                    }
                    if (countGreaterMaxUrnNow != countGreaterMaxUrn)//Вывести число переполненных урн при изменении
                    {
                        countGreaterMaxUrn = countGreaterMaxUrnNow;
                        Console.WriteLine($"\nПереполнено {countGreaterMaxUrn} из {urns.Count} урн. Время {time}");
                    }
                    for (int i = 0; i < containers.Count; i++)//Вывести число переполненных контейнеров при изменении
                    {
                        if (containers[i].volumeNow > containers[i].volumeMax) countGreaterMaxContainersNow++;
                        if (containers[i].volumeNow > valueMaxContainer)
                        {
                            valueMaxContainer = containers[i].volumeNow;
                        }
                    }
                    if (countGreaterMaxContainersNow != countGreaterMaxContainers)
                    {
                        countGreaterMaxContainers = countGreaterMaxContainersNow;
                        Console.WriteLine($"Переполнено {countGreaterMaxContainers} из {containers.Count} контейнеров. Время {time}");
                    }
                    Console.WriteLine($"Максимальная заполненность урн [{urns.Count}]: {valueMaxUrn} Время {time}");
                    Console.WriteLine($"Максимальная заполненность контейнеров [{containers.Count}]: {valueMaxContainer} Время {time}");
                    Console.WriteLine($"Заполненность свалки: {dumpi.volumeNow} Время {time}");
                    double sumJanitor = 0;
                    for(int i=0;i<janitor.Count; i++)
                    {
                        sumJanitor += janitor[i].volumeNow;
                    }
                    Console.WriteLine($"Стредняя заполненность дворников [{janitor.Count}]: {sumJanitor/ janitor.Count} Время {time}");
                    double sumTruck = 0;
                    for (int i = 0; i < truck.Count; i++)
                    {
                        sumTruck += truck[i].volumeNow;
                    }
                    Console.WriteLine($"Стредняя заполненность мусоровозов [{truck.Count}]: {sumTruck / truck.Count} Время {time}\n");
                }
            }
        }

    }

    #region Janitor
    [CustomStyle(
        @"new ol.style.Style({
            image: new ol.style.Circle({
                opacity: 1.0,
                scale: 3.0,
                radius: 3,
                fill: new ol.style.Fill({
                    color: 'rgba(52, 189, 192, 1)'
                }),
                stroke: new ol.style.Stroke({
                    color: 'rgba(44, 117, 119, 1)',
                    width: 1
                }),
            })
        });
        ")]
    class Janitor : Point //Мусоровоз
    {

        public int idСontainer { get; set; }
        public Coordinate сoordinateСontainer { get; set; }
        public List<int> indexUrns { get; set; }
        public int index { get; set; }
        public double volumeNow { get; set; }
        public double volumeMax { get; set; }

        public Janitor(Coordinate coordinate, Coordinate сoordinateСontainer, int idСontainer, double volumeMax) : base(new Coordinate(coordinate))
        {
            this.сoordinateСontainer = сoordinateСontainer;
            indexUrns = new List<int>();
            index = 0;
            this.idСontainer = idСontainer;
            this.volumeMax = volumeMax;
        }

        //Перемещение
        public void move(Coordinate destinationCoordinate, double speed)
        {
            //Вычисляем текущий вектор
            Vector2D vector = new Vector2D(Coordinate, destinationCoordinate);
            //Вычисляем вектор перемещения
            Vector2D vectorTravel = vector.Normalize().Multiply(speed);
            //Действия уборщика
            if (vector.Length() < vectorTravel.Length())//Уборщик прибыл
            {
                X = destinationCoordinate.X;
                Y = destinationCoordinate.Y;
            }
            else //Продолжает движение
            {
                //Вычисляем координату перемещения (начало вектора после перемещения)
                X = X + vectorTravel.X;
                Y = Y + vectorTravel.Y;
            }
        }

        public void put(double garbage)
        {
            volumeNow += garbage;
        }

        public void output(double garbage)
        {
            volumeNow -= garbage;
        }
    }
    #endregion

    #region GarbageTruck
    [CustomStyle(
        @"new ol.style.Style({
            image: new ol.style.Circle({
                opacity: 1.0,
                scale: 5.0,
                radius: 5,
                fill: new ol.style.Fill({
                    color: 'rgba(58, 64, 175, 1)'
                }),
                stroke: new ol.style.Stroke({
                    color: 'rgba(34, 39, 113, 1)',
                    width: 1
                }),
            })
        });
        ")]
    class GarbageTruck : Point //Мусоровоз
    {
        
        public Coordinate сoordinateDump { get; set; }
        public List<int> indexСontainers { get; set; }
        public int index { get; set; }
        public double volumeNow { get; set; }
        public double volumeMax { get; set; }

        public GarbageTruck(Coordinate coordinate, Coordinate сoordinateDump, double volumeMax) : base(new Coordinate(coordinate))
        {
            this.сoordinateDump = сoordinateDump;
            indexСontainers = new List<int>();
            index = 0;
            this.volumeMax = volumeMax;
        }

        //Перемещение
        public void move(Coordinate destinationCoordinate, double speed)
        {
            //Вычисляем текущий вектор
            Vector2D vector = new Vector2D(Coordinate, destinationCoordinate);
            //Вычисляем вектор перемещения
            Vector2D vectorTravel = vector.Normalize().Multiply(speed);
            //Действия мусоровоза
            if (vector.Length() < vectorTravel.Length())//Мусоровоз прибыл прибыл
            {
                X = destinationCoordinate.X;
                Y = destinationCoordinate.Y;
            } else //Продолжает движение
            {
                //Вычисляем координату перемещения (начало вектора после перемещения)
                X = X + vectorTravel.X;
                Y = Y + vectorTravel.Y;
            }
        }

        public void put(double garbage)
        {
            volumeNow += garbage;
        }

        public void output(double garbage)
        {
            volumeNow -= garbage;
        }
    }
    #endregion

    #region GarbageUrn
    [CustomStyle(
        @"new ol.style.Style({
            fill: new ol.style.Fill(
            {
                    color: 'rgba(76, 191, 63, 1)'
            }),
            stroke: new ol.style.Stroke(
            {
                    color: 'rgba(38, 114, 45, 1)',
                    width: 1
            }),
        });
        ")]
    class GarbageUrn : Polygon//Урна
    {
        static double size = 0.0001;
        public double X { get; }
        public double Y { get; }

        public double volumeNow { get; set; }
        public double volumeMax { get; set; }

        public int indexСontainer { get; set; }
        public GarbageUrn(double X, double Y, int indexСontainer, double volumeMax)
            : base(new LinearRing(
                new Coordinate[] {
                    new Coordinate(MathExtensions.LatLonToSpherMerc(X,Y)),
                    new Coordinate(MathExtensions.LatLonToSpherMerc(X,Y-size)),
                    new Coordinate(MathExtensions.LatLonToSpherMerc(X-size,Y-size)),
                    new Coordinate(MathExtensions.LatLonToSpherMerc(X-size,Y)),
                    new Coordinate(MathExtensions.LatLonToSpherMerc(X,Y))
            }))
        {
            this.X = X;
            this.Y = Y;
            this.indexСontainer = indexСontainer;
            this.volumeMax = volumeMax;
        }

        public void put(double garbage)
        {
            volumeNow += garbage;
        }

        public void output(double garbage)
        {
            volumeNow -=garbage;
        }
    }
    #endregion

    #region GarbageСontainer
    [CustomStyle(
        @"new ol.style.Style({
            fill: new ol.style.Fill(
            {
                    color: 'rgba(180, 191, 63, 1)'
            }),
            stroke: new ol.style.Stroke(
            {
                    color: 'rgba(117, 123, 44, 1)',
                    width: 1
            }),
        });
        ")]
    class GarbageСontainer : Polygon//Контейнер
    {
        static double size = 0.0002;
        public double X { get; }
        public double Y { get; }

        public double volumeNow { get; set; }
        public double volumeMax { get; set; }

        public GarbageСontainer(double X, double Y, double volumeMax)
            : base(new LinearRing(
                new Coordinate[] {
                    new Coordinate(MathExtensions.LatLonToSpherMerc(X,Y)),
                    new Coordinate(MathExtensions.LatLonToSpherMerc(X,Y-size)),
                    new Coordinate(MathExtensions.LatLonToSpherMerc(X-size,Y-size)),
                    new Coordinate(MathExtensions.LatLonToSpherMerc(X-size,Y)),
                    new Coordinate(MathExtensions.LatLonToSpherMerc(X,Y))
            }))
        {
            this.X = X;
            this.Y = Y;
            this.volumeMax = volumeMax;
        }
        public void put(double garbage)
        {
            volumeNow += garbage;
        }

        public void output(double garbage)
        {
            volumeNow -= garbage;
        }

    }
    #endregion

    #region GarbageDump
    [CustomStyle(
        @"new ol.style.Style({
            fill: new ol.style.Fill(
            {
                    color: 'rgba(191, 102, 63, 1)'
            }),
            stroke: new ol.style.Stroke(
            {
                    color: 'rgba(138, 73, 44, 1)',
                    width: 1
            }),
        });
        ")]
    class GarbageDump : Polygon//Свалка
    {
        static double size = 0.01;
        public double X { get; }
        public double Y { get; }

        public double volumeNow { get; set; }
        public double volumeMax { get; set; }

        public GarbageDump(double X, double Y)
            : base(new LinearRing(
                new Coordinate[] {
                    new Coordinate(MathExtensions.LatLonToSpherMerc(X,Y)),
                    new Coordinate(MathExtensions.LatLonToSpherMerc(X,Y-size)),
                    new Coordinate(MathExtensions.LatLonToSpherMerc(X-size,Y-size)),
                    new Coordinate(MathExtensions.LatLonToSpherMerc(X-size,Y)),
                    new Coordinate(MathExtensions.LatLonToSpherMerc(X,Y))
            }))
        {
            this.X = X;
            this.Y = Y;
        }
        public void put(double garbage)
        {
            volumeNow += garbage;
        }

        public void output(double garbage)
        {
            volumeNow -= garbage;
        }

    }
    #endregion

    #region GarbageFactory
    [CustomStyle(
        @"new ol.style.Style({
            fill: new ol.style.Fill(
            {
                    color: 'rgba(165, 63, 191, 1)'
            }),
            stroke: new ol.style.Stroke(
            {
                    color: 'rgba(131, 53, 150, 1)',
                    width: 1
            }),
        });
        ")]
    class GarbageFactory : Polygon//Фабрика по переработке
    {
        static double size = 0.001;
        public double X { get; }
        public double Y { get; }

        public double volumeNow { get; set; }
        public double volumeMax { get; set; }

        public GarbageFactory(double X, double Y)
            : base(new LinearRing(
                new Coordinate[] {
                    new Coordinate(MathExtensions.LatLonToSpherMerc(X,Y)),
                    new Coordinate(MathExtensions.LatLonToSpherMerc(X,Y-size)),
                    new Coordinate(MathExtensions.LatLonToSpherMerc(X-size,Y-size)),
                    new Coordinate(MathExtensions.LatLonToSpherMerc(X-size,Y)),
                    new Coordinate(MathExtensions.LatLonToSpherMerc(X,Y))
            }))
        {
            this.X = X;
            this.Y = Y;
        }

        void put(double garbage)
        {
            volumeNow += garbage;
        }

        void output(double garbage)
        {
            volumeNow -=garbage;
        }
    }
    #endregion
}

