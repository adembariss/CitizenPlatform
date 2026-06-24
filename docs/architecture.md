# Architecture

CitizenPlatform, Clean Architecture prensiplerini Modular Monolith yaklaşımıyla birleştirir.

- Domain katmanı iş kurallarını ve domain eventleri içerir.
- Application katmanı use case, DTO, validation ve port arayüzlerini tutar.
- Infrastructure katmanı persistence, identity, storage, geospatial ve notification adaptörlerini içerir.
- Integrations katmanı belediye veritabanı ve outbox entegrasyon sınırlarını barındırır.
- Api ve Worker uç projeleri Application katmanını dış dünyaya açar.
