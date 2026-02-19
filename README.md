## Цель лабораторной работы
Реализация минимального микросервисного бэкенда с кэшированием

## Технологии и требования

- .NET 8
- Redis
- Bogus 

## Что я сделал в проекте

В рамках этой лабораторной работы реализовано и настроено следующее:

- HTTP-эндпоинт `GET /course?id={id}` в сервисе `GenerationService`, возвращающий DTO `Course`.
- Механизм генерации данных курсов (используется Bogus).,
- Кэширование ответов через `IDistributedCache` с провайдером Redis: ключи `course:{id}`, TTL 10 минут.
- Набор расширений `ServiceDefaults` для единообразной конфигурации сервисов: OpenTelemetry (метрики/трейсы), health checks и настройки `HttpClient`.

<img width="1710" height="856" alt="Снимок экрана 2026-02-19 162728" src="https://github.com/user-attachments/assets/056722ed-6bd5-4d50-901b-162b28aa8658" />
<img width="1798" height="857" alt="Снимок экрана 2026-02-19 162815" src="https://github.com/user-attachments/assets/77731ef9-bc02-474c-beb1-71675b622823" />
<img width="1892" height="415" alt="Снимок экрана 2026-02-19 162752" src="https://github.com/user-attachments/assets/dacf45d9-f52d-4d36-acf2-9b1e5514aad9" />
<img width="1896" height="616" alt="Снимок экрана 2026-02-19 162917" src="https://github.com/user-attachments/assets/fd97f650-d3a6-4124-913a-7dde5fc5482a" />






