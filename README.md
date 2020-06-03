# spprGarbageCollector
Контрольная работа по СППР. Контейнер для мусора (вкл. мусорные урны) и отходы.

Прогрмммный модуль представлен фалом GarbageCollector.dll
Версия netcoreapp3.1

При запуске модуля в командной строке требуется ввести число различных элементов. 
Значения по умолчанию указаны в скобках. 
При ошибке ввода или при отсутствии данных будут использоваться данные по умолчанию.
Элементы для ввода:
 - количество мусоровозов
 - количество контейнеров
 - количество уборщиков на один контейнеров
 - количество урн на один контейнер
 
 После ввода всех данных пользователь может просмотреть карту, 
 на которой расположены объекты:
 - свалка (коричневый прямоугольник);
 - фабрика по переработке (фиолетовый прямоугольник);
 - мусоровозы (синие точки);
 - уборщики (бирюзовые точки);
 - контейнеры (желтые прямоугольники);
 - урны (зеленые прямоугольники).
 
Объекты взаимодействуют друг с другом, 
в результате чего выводятся данные описывающие их текущее состояние, 
а также возможные выходы за границы.
Видео с примером работы: видео по ссылке: 
https://drive.google.com/open?id=1ug3OmygtoGMK6-7Xw3GsQRjMWAfVIrmn
