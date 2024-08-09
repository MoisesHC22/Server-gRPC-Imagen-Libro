using Google.Protobuf.WellKnownTypes;
using gRPC.Libro.Serve.Persistencia;
using Grpc.Core;
using MongoDB.Bson;
using MongoDB.Driver;

namespace gRPC.Libro.Serve.Services
{
    public class ImagenService : LibroImg.LibroImgBase
    {
        private readonly IMongoCollection<BsonDocument> _imagenesCollection;

        public ImagenService(MongoDBSetting mongoDBSetting)
        {
            var client = new MongoClient(mongoDBSetting.DefaultConnection);
            var database = client.GetDatabase(mongoDBSetting.Database);
            _imagenesCollection = database.GetCollection<BsonDocument>(mongoDBSetting.Collection);
        }

        public override async Task<Respuesta> GuardarImg(ImgRequest request, ServerCallContext context) 
        {
            var respuesta = new Respuesta();

            try 
            {
                string base64String = request.Img.Trim();

                if (base64String.Length % 4 != 0)
                {
                    base64String = base64String.PadRight(base64String.Length + (4 - base64String.Length % 4), '=');
                }

                var imgData = Convert.FromBase64String(request.Img);

                var imgDocument = new BsonDocument
                {
                    { "id", request.Id },
                    { "img", request.Img }
                };

                await _imagenesCollection.InsertOneAsync(imgDocument);
                respuesta.Mensaje = "La imagen se guardo exitosamente";
            }
            catch (Exception ex)
            {
                respuesta.Mensaje = $"Ocurrio un error al guardar la imagen: {ex.Message}";
                Console.WriteLine($"Error al guardar la imagen: {ex.Message}");
            }

            return respuesta;
        }







        public override async Task<ImgResponse> ConsultaCompleta(Empty request, ServerCallContext context)
        {
            var datos = new ImgResponse();
            try
            {
                var imagenes = await _imagenesCollection.Find(new BsonDocument()).ToListAsync();

                foreach (var imagen in imagenes)
                {
                    datos.Imagenes.Add(new ImgRequest
                    {
                        Id = imagen["id"].AsString,
                        Img = imagen["img"].AsString
                    });
                }
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.Unknown, "Ocurrio un error"), ex.Message);
            }
            return datos;
        }







        public override async Task<ImgRequest> ConsultaFiltro(IdImg request, ServerCallContext context)
        {
            try
            {
                var filtro = Builders<BsonDocument>.Filter.Eq("id", request.Id);
                var imagen = await _imagenesCollection.Find(filtro).FirstOrDefaultAsync();

                if (imagen == null)
                {
                    throw new RpcException(new Status(StatusCode.NotFound, "Imagen no encontrada"));
                }

                return new ImgRequest
                {
                    Id = imagen["id"].AsString,
                    Img = imagen["img"].AsString
                };
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.Unknown, "Ocurrio un error:"), ex.Message);
            }
        }


    }
}
