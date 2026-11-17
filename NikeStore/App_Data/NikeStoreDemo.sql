-- NikeStoreDemo / SQL Server. The application runs this batch in a single transaction.
-- Leave existing databases untouched. Re-running on a complete schema keeps the saved data.
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF EXISTS (SELECT 1 FROM sys.tables WHERE is_ms_shipped = 0)
BEGIN
    IF NOT EXISTS (
        SELECT required.name
        FROM (VALUES ('cart_details'), ('carts'), ('categories'), ('colors'), ('coupons'), ('inventory_transactions'), ('order_details'), ('orders'), ('product_categories'), ('product_colors'), ('product_images'), ('product_variants'), ('products'), ('roles'), ('sizes'), ('users')) required(name)
        WHERE OBJECT_ID(N'dbo.' + required.name, N'U') IS NULL
    ) RETURN;
    THROW 50001, 'NikeStoreDemo contains a different or incomplete schema. No tables were changed.', 1;
END;

CREATE TABLE dbo.[cart_details] (
    [cart_detail_id] int IDENTITY(1,1) NOT NULL,
    [cart_id] int NOT NULL,
    [variant_id] int NOT NULL,
    [quantity] int NULL,
    [added_at] datetime2 NULL,
    CONSTRAINT PK_cart_details PRIMARY KEY ([cart_detail_id])
);

CREATE TABLE dbo.[carts] (
    [cart_id] int IDENTITY(1,1) NOT NULL,
    [user_id] int NOT NULL,
    [created_at] datetime2 NULL,
    [updated_at] datetime2 NULL,
    CONSTRAINT PK_carts PRIMARY KEY ([cart_id])
);

CREATE TABLE dbo.[categories] (
    [category_id] int IDENTITY(1,1) NOT NULL,
    [category_name] varchar(100) NOT NULL,
    [slug] varchar(120) NOT NULL,
    [description] varchar(500) NULL,
    [display_order] int NULL,
    [is_active] bit NULL,
    [created_at] datetime2 NULL,
    [updated_at] datetime2 NULL,
    CONSTRAINT PK_categories PRIMARY KEY ([category_id])
);

CREATE TABLE dbo.[colors] (
    [color_id] int IDENTITY(1,1) NOT NULL,
    [color_name] varchar(50) NOT NULL,
    [color_code] varchar(7) NULL,
    [is_active] bit NULL,
    CONSTRAINT PK_colors PRIMARY KEY ([color_id])
);

CREATE TABLE dbo.[coupons] (
    [coupon_id] int IDENTITY(1,1) NOT NULL,
    [code] varchar(30) NOT NULL,
    [discount_type] varchar(10) NOT NULL,
    [discount_value] decimal(18,2) NOT NULL,
    [max_discount] decimal(18,2) NULL,
    [min_order_value] decimal(18,2) NULL,
    [start_at] datetime NOT NULL,
    [end_at] datetime NOT NULL,
    [usage_limit] int NULL,
    [used_count] int NULL,
    [is_active] bit NULL,
    [created_at] datetime2 NULL,
    [updated_at] datetime2 NULL,
    CONSTRAINT PK_coupons PRIMARY KEY ([coupon_id])
);

CREATE TABLE dbo.[inventory_transactions] (
    [transaction_id] int IDENTITY(1,1) NOT NULL,
    [variant_id] int NOT NULL,
    [user_id] int NULL,
    [order_id] int NULL,
    [transaction_type] varchar(20) NOT NULL,
    [quantity_change] int NOT NULL,
    [note] varchar(255) NULL,
    [created_at] datetime2 NULL,
    CONSTRAINT PK_inventory_transactions PRIMARY KEY ([transaction_id])
);

CREATE TABLE dbo.[order_details] (
    [order_detail_id] int IDENTITY(1,1) NOT NULL,
    [order_id] int NOT NULL,
    [variant_id] int NOT NULL,
    [product_name] varchar(180) NOT NULL,
    [size_value] decimal(4,1) NOT NULL,
    [color_name] varchar(50) NOT NULL,
    [unit_price] decimal(18,2) NOT NULL,
    [quantity] int NOT NULL,
    CONSTRAINT PK_order_details PRIMARY KEY ([order_detail_id])
);

CREATE TABLE dbo.[orders] (
    [order_id] int IDENTITY(1,1) NOT NULL,
    [order_code] varchar(30) NOT NULL,
    [customer_id] int NULL,
    [handled_by_user_id] int NULL,
    [coupon_id] int NULL,
    [order_date] datetime2 NULL,
    [recipient_name] varchar(100) NOT NULL,
    [recipient_phone] varchar(20) NOT NULL,
    [shipping_address] varchar(300) NOT NULL,
    [subtotal] decimal(18,2) NOT NULL,
    [discount_amount] decimal(18,2) NULL,
    [shipping_fee] decimal(18,2) NULL,
    [total_amount] decimal(18,2) NOT NULL,
    [order_status] varchar(20) NULL,
    [payment_method] varchar(20) NULL,
    [payment_status] varchar(20) NULL,
    [note] varchar(500) NULL,
    [updated_at] datetime2 NULL,
    CONSTRAINT PK_orders PRIMARY KEY ([order_id])
);

CREATE TABLE dbo.[product_categories] (
    [product_id] int NOT NULL,
    [category_id] int NOT NULL,
    CONSTRAINT PK_product_categories PRIMARY KEY ([product_id], [category_id])
);

CREATE TABLE dbo.[product_colors] (
    [product_color_id] int IDENTITY(1,1) NOT NULL,
    [product_id] int NOT NULL,
    [color_id] int NOT NULL,
    [is_active] bit NULL,
    CONSTRAINT PK_product_colors PRIMARY KEY ([product_color_id])
);

CREATE TABLE dbo.[product_images] (
    [image_id] int IDENTITY(1,1) NOT NULL,
    [product_id] int NOT NULL,
    [product_color_id] int NOT NULL,
    [image_angle] varchar(10) NOT NULL,
    [image_url] varchar(500) NOT NULL,
    [is_primary] bit NULL,
    [display_order] int NULL,
    [created_at] datetime2 NULL,
    CONSTRAINT PK_product_images PRIMARY KEY ([image_id])
);

CREATE TABLE dbo.[product_variants] (
    [variant_id] int IDENTITY(1,1) NOT NULL,
    [product_id] int NOT NULL,
    [size_id] int NOT NULL,
    [product_color_id] int NOT NULL,
    [sku] varchar(50) NOT NULL,
    [stock_quantity] int NULL,
    [is_active] bit NULL,
    CONSTRAINT PK_product_variants PRIMARY KEY ([variant_id])
);

CREATE TABLE dbo.[products] (
    [product_id] int IDENTITY(1,1) NOT NULL,
    [product_code] varchar(30) NOT NULL,
    [product_name] varchar(180) NOT NULL,
    [slug] varchar(200) NOT NULL,
    [description] nvarchar(max) NULL,
    [price] decimal(18,2) NOT NULL,
    [is_active] bit NULL,
    [is_featured] bit NULL,
    [created_at] datetime2 NULL,
    [updated_at] datetime2 NULL,
    CONSTRAINT PK_products PRIMARY KEY ([product_id])
);

CREATE TABLE dbo.[roles] (
    [role_id] int IDENTITY(1,1) NOT NULL,
    [role_name] varchar(30) NOT NULL,
    [description] varchar(255) NULL,
    CONSTRAINT PK_roles PRIMARY KEY ([role_id])
);

CREATE TABLE dbo.[sizes] (
    [size_id] int IDENTITY(1,1) NOT NULL,
    [size_value] decimal(4,1) NOT NULL,
    [size_system] varchar(10) NULL,
    [is_active] bit NULL,
    CONSTRAINT PK_sizes PRIMARY KEY ([size_id])
);

CREATE TABLE dbo.[users] (
    [user_id] int IDENTITY(1,1) NOT NULL,
    [role_id] int NOT NULL,
    [full_name] varchar(100) NOT NULL,
    [email] varchar(254) NOT NULL,
    [password_hash] varchar(255) NOT NULL,
    [phone] varchar(20) NULL,
    [address] varchar(300) NULL,
    [is_active] bit NULL,
    [created_at] datetime2 NULL,
    [updated_at] datetime2 NULL,
    CONSTRAINT PK_users PRIMARY KEY ([user_id])
);

ALTER TABLE dbo.[cart_details] ADD CONSTRAINT [FK_cart_details_cart_id] FOREIGN KEY ([cart_id]) REFERENCES dbo.[carts] ([cart_id]) ON DELETE CASCADE;
ALTER TABLE dbo.[cart_details] ADD CONSTRAINT [FK_cart_details_variant_id] FOREIGN KEY ([variant_id]) REFERENCES dbo.[product_variants] ([variant_id]);
ALTER TABLE dbo.[carts] ADD CONSTRAINT [FK_carts_user_id] FOREIGN KEY ([user_id]) REFERENCES dbo.[users] ([user_id]) ON DELETE CASCADE;
ALTER TABLE dbo.[inventory_transactions] ADD CONSTRAINT [FK_inventory_transactions_order_id] FOREIGN KEY ([order_id]) REFERENCES dbo.[orders] ([order_id]);
ALTER TABLE dbo.[inventory_transactions] ADD CONSTRAINT [FK_inventory_transactions_user_id] FOREIGN KEY ([user_id]) REFERENCES dbo.[users] ([user_id]);
ALTER TABLE dbo.[inventory_transactions] ADD CONSTRAINT [FK_inventory_transactions_variant_id] FOREIGN KEY ([variant_id]) REFERENCES dbo.[product_variants] ([variant_id]);
ALTER TABLE dbo.[order_details] ADD CONSTRAINT [FK_order_details_order_id] FOREIGN KEY ([order_id]) REFERENCES dbo.[orders] ([order_id]);
ALTER TABLE dbo.[order_details] ADD CONSTRAINT [FK_order_details_variant_id] FOREIGN KEY ([variant_id]) REFERENCES dbo.[product_variants] ([variant_id]);
ALTER TABLE dbo.[orders] ADD CONSTRAINT [FK_orders_coupon_id] FOREIGN KEY ([coupon_id]) REFERENCES dbo.[coupons] ([coupon_id]);
ALTER TABLE dbo.[orders] ADD CONSTRAINT [FK_orders_customer_id] FOREIGN KEY ([customer_id]) REFERENCES dbo.[users] ([user_id]);
ALTER TABLE dbo.[orders] ADD CONSTRAINT [FK_orders_handled_by_user_id] FOREIGN KEY ([handled_by_user_id]) REFERENCES dbo.[users] ([user_id]);
ALTER TABLE dbo.[product_categories] ADD CONSTRAINT [FK_product_categories_category_id] FOREIGN KEY ([category_id]) REFERENCES dbo.[categories] ([category_id]) ON DELETE CASCADE;
ALTER TABLE dbo.[product_colors] ADD CONSTRAINT [FK_product_colors_color_id] FOREIGN KEY ([color_id]) REFERENCES dbo.[colors] ([color_id]);
ALTER TABLE dbo.[product_categories] ADD CONSTRAINT [FK_product_categories_product_id] FOREIGN KEY ([product_id]) REFERENCES dbo.[products] ([product_id]) ON DELETE CASCADE;
ALTER TABLE dbo.[product_colors] ADD CONSTRAINT [FK_product_colors_product_id] FOREIGN KEY ([product_id]) REFERENCES dbo.[products] ([product_id]) ON DELETE CASCADE;
ALTER TABLE dbo.[product_variants] ADD CONSTRAINT [FK_product_variants_product_id] FOREIGN KEY ([product_id]) REFERENCES dbo.[products] ([product_id]);
ALTER TABLE dbo.[product_variants] ADD CONSTRAINT [FK_product_variants_size_id] FOREIGN KEY ([size_id]) REFERENCES dbo.[sizes] ([size_id]);
ALTER TABLE dbo.[users] ADD CONSTRAINT [FK_users_role_id] FOREIGN KEY ([role_id]) REFERENCES dbo.[roles] ([role_id]);

CREATE UNIQUE INDEX UX_users_email ON dbo.[users] ([email]);
CREATE UNIQUE INDEX UX_roles_role_name ON dbo.[roles] ([role_name]);
CREATE UNIQUE INDEX UX_categories_slug ON dbo.[categories] ([slug]);
CREATE UNIQUE INDEX UX_products_product_code ON dbo.[products] ([product_code]);
CREATE UNIQUE INDEX UX_products_slug ON dbo.[products] ([slug]);
CREATE UNIQUE INDEX UX_product_variants_sku ON dbo.[product_variants] ([sku]);
CREATE UNIQUE INDEX UX_orders_order_code ON dbo.[orders] ([order_code]);
CREATE UNIQUE INDEX UX_coupons_code ON dbo.[coupons] ([code]);

INSERT dbo.roles (role_name) VALUES ('Admin'), ('Employee'), ('Customer');
INSERT dbo.categories (category_name, slug, display_order, is_active, created_at) VALUES
    ('Men Shoes','men-shoes',1,1,SYSDATETIME()), ('Women Shoes','women-shoes',2,1,SYSDATETIME()),
    ('Kids Shoes','kids-shoes',3,1,SYSDATETIME()), ('Running','running',4,1,SYSDATETIME()),
    ('Jordan','jordan',5,1,SYSDATETIME()), ('Lifestyle','lifestyle',6,1,SYSDATETIME());
INSERT dbo.colors (color_name, color_code, is_active) VALUES
    ('Black','#202020',1), ('White','#FFFFFF',1), ('Red','#B52A32',1), ('Blue','#326EA8',1), ('Pink','#E6A5C1',1), ('Grey','#808080',1);
INSERT dbo.sizes (size_value, size_system, is_active) VALUES
    (36,'EU',1),(37,'EU',1),(38,'EU',1),(39,'EU',1),(40,'EU',1),(41,'EU',1),(42,'EU',1);

-- Sample catalogue (VND). Image paths refer to files supplied with the project.
DECLARE @catalog TABLE (code varchar(30), name varchar(180), price decimal(18,2), audience varchar(100), collection varchar(100), featured bit);
INSERT @catalog VALUES
    ('AJ1LOW-M', 'Air Jordan 1 Low', 3299000, 'Men Shoes', 'Jordan', 1),
    ('AJ1LOWALT-K', 'Air Jordan 1 Low Alt', 1999000, 'Kids Shoes', 'Jordan', 0),
    ('AJ1MID-K', 'Air Jordan 1 Mid Kids', 2199000, 'Kids Shoes', 'Jordan', 0),
    ('AJ1MID-M', 'Air Jordan 1 Mid', 3799000, 'Men Shoes', 'Jordan', 0),
    ('JSPIZIKELOW-M', 'Jordan Spizike Low', 4199000, 'Men Shoes', 'Jordan', 0),
    ('NK-AF107-M', 'Nike Air Force 1 ''07', 2929000, 'Men Shoes', 'Lifestyle', 1),
    ('NK-AF1ESS-W', 'Nike Air Force 1 Essential', 2929000, 'Women Shoes', 'Lifestyle', 0),
    ('NK-AM90-K', 'Nike Air Max 90 Kids', 2499000, 'Kids Shoes', 'Lifestyle', 0),
    ('NK-AM90-M', 'Nike Air Max 90', 3799000, 'Men Shoes', 'Lifestyle', 0),
    ('NK-AMMUSE-W', 'Nike Air Max Muse', 4199000, 'Women Shoes', 'Lifestyle', 0),
    ('NK-CORTEZSE-W', 'Nike Cortez SE', 2499000, 'Women Shoes', 'Lifestyle', 0),
    ('NK-P6000-M', 'Nike P-6000', 2929000, 'Men Shoes', 'Lifestyle', 0),
    ('NK-PEG42-M', 'Nike Pegasus 42', 3899000, 'Men Shoes', 'Running', 0),
    ('NK-PEG42-W', 'Nike Pegasus 42 Women', 3899000, 'Women Shoes', 'Running', 1),
    ('NK-REV8-M', 'Nike Revolution 8', 1699000, 'Men Shoes', 'Running', 0),
    ('NK-SHOXNZ-M', 'Nike Shox NZ', 3999000, 'Men Shoes', 'Lifestyle', 0),
    ('NK-V5RNR-K', 'Nike V5 RNR Kids', 1899000, 'Kids Shoes', 'Lifestyle', 1),
    ('NK-V5RNRPRE-W', 'Nike V5 RNR Premium', 2799000, 'Women Shoes', 'Lifestyle', 0),
    ('NK-VOM18-M', 'Nike Vomero 18', 4499000, 'Men Shoes', 'Running', 0),
    ('NK-VOM5-K', 'Nike Zoom Vomero 5 Kids', 2499000, 'Kids Shoes', 'Running', 0);
INSERT dbo.products (product_code, product_name, slug, price, is_active, is_featured, created_at)
SELECT code, name, LOWER(code), price, 1, featured, SYSDATETIME() FROM @catalog;

INSERT dbo.product_categories (product_id, category_id)
SELECT p.product_id, c.category_id FROM @catalog d
JOIN dbo.products p ON p.product_code = d.code
JOIN dbo.categories c ON c.category_name IN (d.audience, d.collection);

DECLARE @photos TABLE (code varchar(30), color varchar(50), top_file varchar(50));
INSERT @photos VALUES
    ('AJ1LOW-M', 'black', 'top.jpg'),
    ('AJ1LOW-M', 'red', 'top.jpg'),
    ('AJ1LOW-M', 'white', 'top.jpg'),
    ('AJ1LOWALT-K', 'black', 'top.jpg'),
    ('AJ1LOWALT-K', 'red', 'top.jpg'),
    ('AJ1LOWALT-K', 'white', 'top.jpg'),
    ('AJ1MID-K', 'black', 'top.jpg'),
    ('AJ1MID-K', 'red', 'top.jpg'),
    ('AJ1MID-K', 'white', 'top.jpg'),
    ('AJ1MID-M', 'black', 'top.jpg'),
    ('AJ1MID-M', 'red', 'top.jpg'),
    ('AJ1MID-M', 'white', 'top.jpg'),
    ('JSPIZIKELOW-M', 'black', 'top.jpg'),
    ('JSPIZIKELOW-M', 'red', 'top.jpg'),
    ('JSPIZIKELOW-M', 'white', 'top.jpg'),
    ('NK-AF107-M', 'black', 'top.jpg'),
    ('NK-AF107-M', 'grey', 'top.jpg'),
    ('NK-AF107-M', 'white', 'top.jpg'),
    ('NK-AF1ESS-W', 'black', 'top.jpg'),
    ('NK-AF1ESS-W', 'pink', 'top.jpg'),
    ('NK-AF1ESS-W', 'white', 'top.jpg'),
    ('NK-AM90-K', 'black', 'top.jpg'),
    ('NK-AM90-K', 'blue', 'tp[.jpg'),
    ('NK-AM90-K', 'white', 'top.jpg'),
    ('NK-AM90-M', 'black', 'top.jpg'),
    ('NK-AM90-M', 'grey', 'top.jpg'),
    ('NK-AM90-M', 'white', 'top.jpg'),
    ('NK-AMMUSE-W', 'black', 'top.jpg'),
    ('NK-AMMUSE-W', 'pink', 'top.jpg'),
    ('NK-AMMUSE-W', 'white', 'top.jpg'),
    ('NK-CORTEZSE-W', 'black', 'top.jpg'),
    ('NK-CORTEZSE-W', 'pink', 'top.jpg'),
    ('NK-CORTEZSE-W', 'white', 'top.jpg'),
    ('NK-P6000-M', 'black', 'top.jpg'),
    ('NK-P6000-M', 'grey', 'top.jpg'),
    ('NK-P6000-M', 'white', 'top.jpg'),
    ('NK-PEG42-M', 'black', 'top.jpg'),
    ('NK-PEG42-M', 'blue', 'top.jpg'),
    ('NK-PEG42-M', 'white', 'top.jpg'),
    ('NK-PEG42-W', 'black', 'top.jpg'),
    ('NK-PEG42-W', 'blue', 'top.jpg'),
    ('NK-PEG42-W', 'white', 'top.jpg'),
    ('NK-REV8-M', 'black', 'top.jpg'),
    ('NK-REV8-M', 'blue', 'top.jpg'),
    ('NK-REV8-M', 'white', 'top.jpg'),
    ('NK-SHOXNZ-M', 'black', 'top.jpg'),
    ('NK-SHOXNZ-M', 'grey', 'top.jpg'),
    ('NK-SHOXNZ-M', 'white', 'top.jpg'),
    ('NK-V5RNR-K', 'black', 'top.jpg'),
    ('NK-V5RNR-K', 'blue', 'top.jpg'),
    ('NK-V5RNR-K', 'white', 'top.jpg'),
    ('NK-V5RNRPRE-W', 'black', 'top.jpg'),
    ('NK-V5RNRPRE-W', 'pink', 'top.jpg'),
    ('NK-V5RNRPRE-W', 'white', 'top.jpg'),
    ('NK-VOM18-M', 'black', 'top.jpg'),
    ('NK-VOM18-M', 'blue', 'top.jpg'),
    ('NK-VOM18-M', 'white', 'top.jpg'),
    ('NK-VOM5-K', 'black', 'top.jpg'),
    ('NK-VOM5-K', 'blue', 'top.jpg'),
    ('NK-VOM5-K', 'white', 'top.jpg');
INSERT dbo.product_colors (product_id, color_id, is_active)
SELECT p.product_id, c.color_id, 1 FROM @photos d
JOIN dbo.products p ON p.product_code = d.code JOIN dbo.colors c ON LOWER(c.color_name) = d.color;
INSERT dbo.product_variants (product_id, product_color_id, size_id, sku, stock_quantity, is_active)
SELECT p.product_id, pc.product_color_id, s.size_id,
       p.product_code + '-' + UPPER(c.color_name) + '-' + CONVERT(varchar(5), CONVERT(int, s.size_value)), 12, 1
FROM dbo.product_colors pc JOIN dbo.products p ON p.product_id = pc.product_id
JOIN dbo.colors c ON c.color_id = pc.color_id CROSS JOIN dbo.sizes s;
INSERT dbo.product_images (product_id, product_color_id, image_angle, image_url, is_primary, display_order, created_at)
SELECT p.product_id, pc.product_color_id, img.angle,
       '~/Content/Images/Products/' + d.code + '/' + d.color + '/' + img.file_name,
       img.is_primary, img.position, SYSDATETIME()
FROM @photos d JOIN dbo.products p ON p.product_code = d.code
JOIN dbo.colors c ON LOWER(c.color_name) = d.color
JOIN dbo.product_colors pc ON p.product_id = pc.product_id AND pc.color_id = c.color_id
CROSS APPLY (VALUES ('Side', 'side.jpg', 1, 0), ('Top', d.top_file, 0, 1)) img(angle, file_name, is_primary, position);

INSERT dbo.users (role_id, full_name, email, password_hash, is_active, created_at)
SELECT role_id, 'Demo Admin', 'admin@nikestore.test', 'PBKDF2-SHA256$nQ3AbZoRqfbKzvuQe93Ngw==$SnlrAXivn/hBGJupKDvaYE5LqzT6VKgYlRN1XvQOPN8=', 1, SYSDATETIME() FROM dbo.roles WHERE role_name = 'Admin';
INSERT dbo.users (role_id, full_name, email, password_hash, is_active, created_at)
SELECT role_id, 'Demo Employee', 'staff@nikestore.test', 'PBKDF2-SHA256$m8adLyYXJQ4a43YnAN4YNw==$4YSBBRntM1343j4IrYne1vq1PkEXPjrIDHtbDr9OfsM=', 1, SYSDATETIME() FROM dbo.roles WHERE role_name = 'Employee';
