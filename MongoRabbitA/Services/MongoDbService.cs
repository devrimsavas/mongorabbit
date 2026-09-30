using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoRabbitA.Models;
using MongoRabbitA.DTOs.ProductDTOs;

namespace MongoRabbitA.Services
{
    public class MongoDbService
    {
        //injection 
        private readonly IMongoCollection<Product> _products;
        public MongoDbService(IConfiguration config)
        {
            var connectionString=config.GetSection("MongoDB:ConnectionString").Value;
            var databaseName=config.GetSection("MongoDB:DatabaseName").Value;
            var client=new MongoClient(connectionString);
            var database=client.GetDatabase(databaseName);
            _products=database.GetCollection<Product>("productcollection");
        }

        //count document 
        public async Task<long> CountDocumentAsync()
        {
            var filter=Builders<Product>.Filter.Empty;
            var documentCount=await _products.CountDocumentsAsync(filter);
            return documentCount;

        }

        //find all 
        public async Task<List<Product>> GetAllProductsAsync(int pageSize=10,int pageNumber=1)
        {
            var safePageNumber=Math.Max(1,pageNumber);
            var safePageSize=Math.Clamp(pageSize,1,100);
            
            var products=await _products
           .Find(Builders<Product>.Filter.Empty) //all 
           .Skip((safePageNumber-1)*safePageSize)
           .Limit(safePageSize)
           .ToListAsync();

           return products;
        }
        //quantity stock greater 0 
        public async Task<List<Product>> GetProductInStockAsync()
        {
            //filter 
            var inStockFilter=Builders<Product>.Filter.Gt(p=>p.QuantityInStock,0);
            //apply filter
            var availableProducts=await _products.Find(inStockFilter).ToListAsync();
            return availableProducts;
        }

        //get by id 
        public async Task<Product> GetProductById(string id)
        {
            try
            {
                var existedProduct = await _products.Find(p => p.Id == id).FirstOrDefaultAsync() ?? throw new Exception("Product does not exist");
                return existedProduct;
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<Product?> GetProduct(string name)
        {
            var filter=Builders<Product>.Filter.Regex(p=>p.Name, new MongoDB.Bson.BsonRegularExpression($"^{name}$","i"));
            return await _products.Find(filter).FirstOrDefaultAsync();
            
        }
        public async Task<Product?> UpdateProductAsync(string id, UpdateProductDTO updateProduct)
        {
            var updates=new List<UpdateDefinition<Product>>();
            if (!string.IsNullOrWhiteSpace(updateProduct.NewName))
            {
                updates.Add(Builders<Product>.Update.Set(p=>p.Name,updateProduct.NewName));
            }
            if (updateProduct.NewPrice.HasValue)
            {
                updates.Add(Builders<Product>.Update.Set(p=>p.Price,updateProduct.NewPrice));
            }
            if (updateProduct.NewQuantityInStock.HasValue)
            {
                updates.Add(Builders<Product>.Update.Set(p => p.QuantityInStock, updateProduct.NewQuantityInStock.Value));
            }

            if (!string.IsNullOrWhiteSpace(updateProduct.NewBrand))
            {
                updates.Add(Builders<Product>.Update.Set(p => p.Brand, updateProduct.NewBrand));
            }

            // do not update query if server did not send any field
            if (updates.Count == 0)
            {
                return await _products.Find(p => p.Id == id).FirstOrDefaultAsync();
            }
            var combinedUpdate=Builders<Product>.Update.Combine(updates);
            var options=new FindOneAndUpdateOptions<Product>
            {
                ReturnDocument=ReturnDocument.After
            };
            return await _products.FindOneAndUpdateAsync(p=>p.Id==id,combinedUpdate,options);
            
        }
        //update product amount in stock 
        public async Task<Product?> UpdateStock(string id,int quantity)
        {
            //create filter 
            var filterBuilder=Builders<Product>.Filter;
            var filter=filterBuilder.Eq(p=>p.Id,id);
            //check if quantity pos or negative 
            if (quantity<0)
            {
                filter &=filterBuilder.Gte(p=>p.QuantityInStock,Math.Abs(quantity));
            }
            //update 
            var update=Builders<Product>.Update.Inc(p=>p.QuantityInStock,quantity);
            var options=new FindOneAndUpdateOptions<Product>
            {
                ReturnDocument=ReturnDocument.After
            };
            return await _products.FindOneAndUpdateAsync(filter,update,options);
           
        }
        //get brands 
        public async Task<List<Product>> GetProductSameBrandAsync(string brand)
        {
            if (string.IsNullOrWhiteSpace(brand) )
            {
                throw new ArgumentException("Brand Name cannot be empty",nameof(brand));
            }
            //filter
            var filterBuilder=Builders<Product>.Filter;

            // Escape special regex characters to prevent query parsing errors
            var escapedBrand = System.Text.RegularExpressions.Regex.Escape(brand.Trim());
            var filter = filterBuilder.Regex(p => p.Brand, new MongoDB.Bson.BsonRegularExpression($"^{escapedBrand}$", "i"));
            var products=await _products.Find(filter).ToListAsync();
            return products;
        }

        //delete product  by id 
        public async Task<string?> DeleteProductByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Id cannot be empty", nameof(id));
            }

            var filterBuilder = Builders<Product>.Filter;
            var filter = filterBuilder.Eq(p => p.Id, id);
            var productToDelete = await _products.FindOneAndDeleteAsync(filter);

            return productToDelete?.Name;
        }

        //Single Product insertation 
        public async Task<Product> CreateProductAsync(CreateProductDTO dto)
        {
            var product=new Product
            {
                Name=dto.Name.Trim(),
                Brand=dto.Brand.Trim(),
                Price=dto.Price,
                QuantityInStock=dto.QuantityInStock
            };

            await _products.InsertOneAsync(product);
            return product;
        }
        //bulk product insertation 
        public async Task<IEnumerable<Product>> InsertManyProductAsync(IEnumerable<CreateProductDTO> dtos)
        {
            if (dtos is null || !dtos.Any())
            {
                return [];
            }
            var products=dtos.Select(dto=> new Product
            {
                Name=dto.Name.Trim(),
                Brand=dto.Brand.Trim(),
                Price=dto.Price,
                QuantityInStock=dto.QuantityInStock
            }
            ).ToList();
            if (products.Count>0)
            {
                await _products.InsertManyAsync(products);

            }
            return products;
        }



    }

//update product record
public record UpdateProductDTO(string? NewName,decimal? NewPrice,int? NewQuantityInStock, string? NewBrand);
//Quantity record 
public record UpdateStockDTO(int Quantity);

}

