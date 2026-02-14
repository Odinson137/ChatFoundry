# План: БД в FileService, GraphQL, ключ по id, контроллер только на скачивание

## 1. Ключ в хранилище (Key): только id

**Решение: в Key хранить только `{guid}.{ext}` (или `uploads/{guid}.{ext}`).**

CompanyId и WorkflowId в путь в GCS не включать — они есть только в БД. Так мы не передаём лишние байты по сети (в ответах и в запросах к storage), а фильтрация и доступ всегда идут через БД по CompanyId/WorkflowId. Единственный плюс длинного пути `companyId/workflowId/guid.ext` — возможность листинга в GCS по префиксу без БД; раз список файлов всё равно из БД через GraphQL, смысла в дублировании нет.

---

## 2. API: GraphQL + один REST-эндпоинт

### GraphQL (как в других сервисах)

- **Query**
  - `files(companyId: UUID!, workflowId: UUID)` — список файлов из БД (фильтр по companyId, опционально workflowId). Возвращать тип с полями: id, key, url (собранный из конфига + key), originalFileName, contentType, size, createdAt и т.д. Использовать BaseGraphQl (UserId из JWT), проекции/фильтрацию по необходимости.
- **Mutation**
  - Сохранение информации о файле при загрузке с страницы — через GraphQL. Вариант: мутация `uploadFile(companyId: UUID!, workflowId: UUID, file: Upload!)` по спецификации GraphQL multipart (HotChocolate Upload scalar). Резолвер: принять поток, загрузить в GCS по ключу `uploads/{newGuid}.{ext}`, сохранить в БД запись (Id, UploadedByUserId из JWT, CompanyId, WorkflowId, Key, OriginalFileName, ContentType, Size), вернуть созданную сущность (id, key, url).

### REST-контроллер

- Один эндпоинт: **скачивание файла по id**.
  - Например: `GET /files/{id}/download` (или `GET /files/download/{id}`).
  - По id найти запись в БД, проверить доступ (например, пользователь из той же компании), по полю Key забрать объект из GCS и отдать поток (или редирект на signed URL). Так контроллер не дублирует логику списка/загрузки — только отдача файла по id.

---

## 3. Сущность и БД

- **FileEntity** (наследник EntityBase): Id, CreatedAt, ModifiedAt, **UploadedByUserId**, **CompanyId**, **WorkflowId?**, **Key** (только `uploads/{guid}.{ext}`), **OriginalFileName?**, **ContentType?**, **Size?**.
- URL не хранить: собирать в коде из конфига (PublicBaseUrl или `https://storage.googleapis.com/{bucket}`) + Key.
- FileDbContext, конфигурация EF, репозиторий (или прямой доступ через DbContext в резолверах) — по аналогии с CompanyService/WorkflowService.

---

## 4. Зависимости и регистрация

- FileService: ссылки на Shared.Domain (EntityBase), Shared.Infrastructure (AddPostgreSql, BaseGraphQl, Mutation).
- HotChocolate: AddGraphQLServer(), AddQueryType<Query>(), AddMutationType<Mutation>(), AddTypeExtension<FileMutation>(), AddProjections/Filtering/Sorting. Для мутации загрузки — поддержка Upload (HotChocolate.Data).
- Gateway: маршрут на FileService уже есть; для GraphQL — проксировать запросы на тот же file-service (path для GraphQL обычно /graphql или как в других сервисах).

---

## 5. Сводка

| Что | Реализация |
|-----|------------|
| Ключ в GCS | Только `uploads/{guid}.{ext}` (без companyId/workflowId в пути). |
| Список файлов | GraphQL Query `files(companyId, workflowId?)` — выборка из БД. |
| Сохранение при загрузке с страницы | GraphQL Mutation `uploadFile(companyId, workflowId?, file: Upload!)` — GCS + запись в БД. |
| Скачивание | REST GET `/files/{id}/download` — по id из БД, проверка доступа, отдача файла из GCS по Key. |

После этого получение коллекции файлов и сохранение информации о файле при загрузке выполняются через GraphQL, как в остальных сервисах; по сети передаётся только короткий Key (id/ext); контроллер остаётся с одним эндпоинтом — скачивание по id.
