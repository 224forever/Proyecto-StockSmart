from flask import Flask, request, jsonify
from functools import wraps
import os
import json
from dotenv import load_dotenv
from azure.storage.blob import BlobServiceClient

app = Flask(__name__)

# Cargar variables de entorno desde el archivo .env
load_dotenv()

# Obtener los secretos desde el archivo .env
cosmosdb_endpoint = os.getenv("COSMOSDB_ENDPOINT")
cosmosdb_key = os.getenv("COSMOSDB_KEY")
blob_connection_string = os.getenv("STORAGE_ACCOUNT_CONNECTION_STRING")
API_KEY = os.getenv("STOCKSMART_API_KEY")

# Verificar que las variables de entorno estén definidas
if not all([cosmosdb_endpoint, cosmosdb_key, blob_connection_string, API_KEY]):
    raise ValueError("No se encontraron todos los secretos necesarios en el archivo .env")

# Crear el cliente de Azure Blob Storage
blob_service_client = BlobServiceClient.from_connection_string(blob_connection_string)
container_name = 'data'
blob_name = 'products.json'
container_client = blob_service_client.get_container_client(container_name)

def require_api_key(view_function):
    @wraps(view_function)
    def decorated_function(*args, **kwargs):
        api_key = request.headers.get('X-API-Key')
        if api_key and api_key == API_KEY:
            return view_function(*args, **kwargs)
        else:
            return jsonify({"error": "Unauthorized"}), 401
    return decorated_function

def get_blob_content():
    try:
        blob_client = container_client.get_blob_client(blob_name)
        blob_data = blob_client.download_blob()
        return json.loads(blob_data.readall())
    except Exception as e:
        app.logger.error(f"Error al obtener el contenido del blob: {e}")
        raise

@app.route('/productos/<id>', methods=['GET'])
@require_api_key
def get_product(id):
    try:
        products = get_blob_content()
        product = next((p for p in products if p['ProductID'] == id), None)
        if product:
            return jsonify(product), 200
        else:
            return jsonify({"error": "Product not found"}), 404
    except Exception as e:
        app.logger.error(f"Error al obtener el producto con ID {id}: {e}")
        return jsonify({"error": "Error retrieving product"}), 500

@app.route('/productos/', methods=['GET'])
@require_api_key
def get_products():
    try:
        products = get_blob_content()
        query = request.args
        filtered_products = products

        if 'desc' in query:
            desc = query['desc'].lower()
            filtered_products = [p for p in filtered_products if desc in p.get('ProductName', '').lower()]

        if 'price' in query:
            try:
                min_price, max_price = map(float, query['price'].split('-'))
                filtered_products = [p for p in filtered_products if min_price <= float(p.get('UnitPrice', 0)) <= max_price]
            except ValueError:
                return jsonify({"error": "Invalid price range"}), 400

        return jsonify(filtered_products), 200
    except Exception as e:
        app.logger.error(f"Error al obtener productos: {e}")
        return jsonify({"error": "Error retrieving products"}), 500

@app.route('/productos/', methods=['POST'])
@require_api_key
def create_product():
    try:
        data = request.get_json()
        if not all(k in data for k in ['ProductID', 'ProductName', 'CategoryID', 'QuantityPerUnit', 'UnitPrice', 'UnitsInStock', 'UnitsOnOrder', 'ReorderLevel', 'SupplierID']):
            return jsonify({"error": "Missing data"}), 400

        products = get_blob_content()
        if any(p['ProductID'] == data['ProductID'] for p in products):
            return jsonify({"error": "Product ID already exists"}), 400

        products.append(data)
        blob_client = container_client.get_blob_client(blob_name)
        blob_client.upload_blob(json.dumps(products), overwrite=True)

        return jsonify({"message": "Product added successfully"}), 201
    except Exception as e:
        app.logger.error(f"Error al añadir el producto: {e}")
        return jsonify({"error": "Error adding product"}), 500

@app.route('/productos/<id>', methods=['PUT'])
@require_api_key
def update_product(id):
    try:
        data = request.get_json()
        if not all(k in data for k in ['ProductID', 'ProductName', 'CategoryID', 'QuantityPerUnit', 'UnitPrice', 'UnitsInStock', 'UnitsOnOrder', 'ReorderLevel', 'SupplierID']):
            return jsonify({"error": "Missing data"}), 400

        products = get_blob_content()
        product = next((p for p in products if p['ProductID'] == id), None)
        if not product:
            return jsonify({"error": "Product not found"}), 404

        for key, value in data.items():
            product[key] = value

        blob_client = container_client.get_blob_client(blob_name)
        blob_client.upload_blob(json.dumps(products), overwrite=True)

        return jsonify({"message": "Product updated successfully"}), 200
    except Exception as e:
        app.logger.error(f"Error al actualizar el producto con ID {id}: {e}")
        return jsonify({"error": "Error updating product"}), 500

if __name__ == '__main__':
    app.run(debug=True)
