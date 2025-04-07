using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SimpleWebRequest
{
    public partial class MainWindow : Window
    {
        private List<Product> _products = new List<Product>();
        // Houdt bij welk product momenteel bewerkt wordt
        private Product _selectedProductForEditing = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _products = await GetProductsAsync("https://pmarcelis.mid-ica.nl/products/");
            DataContext = _products;
        }

        // Knop voor zowel toevoegen als updaten
        private async void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            decimal price;
            if (!decimal.TryParse(NewProductPrice.Text, out price))
            {
                MessageBox.Show("Voer een geldige prijs in.");
                return;
            }

            if (_selectedProductForEditing != null)
            {
                // Update-modus: gebruik PUT
                _selectedProductForEditing.Name = NewProductName.Text;
                _selectedProductForEditing.Price = price;
                _selectedProductForEditing.ShortDescription = NewProductDescription.Text;

                var updated = await PutProductAsync("https://pmarcelis.mid-ica.nl/products/?id=" + _selectedProductForEditing.Id, _selectedProductForEditing);
                if (updated != null)
                {
                    int index = _products.FindIndex(p => p.Id == _selectedProductForEditing.Id);
                    if (index >= 0)
                    {
                        _products[index] = updated;
                    }
                    ProductsDataGrid.Items.Refresh();
                }

                // Reset het formulier
                NewProductName.Text = string.Empty;
                NewProductPrice.Text = string.Empty;
                NewProductDescription.Text = string.Empty;
                _selectedProductForEditing = null;
                AddProductButton.Content = "Add Product";
            }
            else
            {
                // Toevoegmodus: gebruik POST
                var newProduct = new Product
                {
                    Name = NewProductName.Text,
                    Price = price,
                    ShortDescription = NewProductDescription.Text
                };

                var created = await PostProductAsync("https://pmarcelis.mid-ica.nl/products/", newProduct);
                if (created != null)
                {
                    _products.Add(created);
                    ProductsDataGrid.Items.Refresh();

                    // Maak de invoervelden leeg
                    NewProductName.Text = string.Empty;
                    NewProductPrice.Text = string.Empty;
                    NewProductDescription.Text = string.Empty;
                }
            }
        }

        // GET de lijst van producten
        private async Task<List<Product>> GetProductsAsync(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Kan producten niet ophalen");
                    return new List<Product>();
                }

                string json = await response.Content.ReadAsStringAsync();
                var wrapper = JsonConvert.DeserializeObject<ApiResponse<List<Product>>>(json);
                return wrapper?.Data ?? new List<Product>();
            }
        }

        // POST een nieuw product
        private async Task<Product> PostProductAsync(string url, Product product)
        {
            using (HttpClient client = new HttpClient())
            {
                var json = JsonConvert.SerializeObject(product);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, content);
                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show($"Fout bij het aanmaken van product: {response.StatusCode}");
                    return null;
                }

                string responseBody = await response.Content.ReadAsStringAsync();
                var returnData = JsonConvert.DeserializeObject<ApiResponse<Product>>(responseBody);
                return returnData?.Data;
            }
        }

        // PUT: Update een bestaand product
        private async Task<Product> PutProductAsync(string url, Product product)
        {
            using (HttpClient client = new HttpClient())
            {
                var json = JsonConvert.SerializeObject(product);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PutAsync(url, content);
                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show($"Fout bij het updaten van product: {response.StatusCode}");
                    return null;
                }

                string responseBody = await response.Content.ReadAsStringAsync();
                var returnData = JsonConvert.DeserializeObject<ApiResponse<Product>>(responseBody);
                return returnData?.Data;
            }
        }

        // DELETE: Verwijder een product
        private async Task<bool> DeleteProductAsync(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                var response = await client.DeleteAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show($"Fout bij het verwijderen van product: {response.StatusCode}");
                    return false;
                }
                return true;
            }
        }

        // Event handler voor de "Bewerken"-knop in de DataGrid
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            if (button?.CommandParameter is Product product)
            {
                // Vul de invoervelden met de productgegevens
                NewProductName.Text = product.Name;
                NewProductPrice.Text = product.Price.ToString();
                NewProductDescription.Text = product.ShortDescription;
                _selectedProductForEditing = product;
                AddProductButton.Content = "Update Product";
            }
        }

        // Event handler voor de "Verwijder"-knop in de DataGrid
        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            if (button?.CommandParameter is Product product)
            {
                if (MessageBox.Show($"Weet je zeker dat je '{product.Name}' wilt verwijderen?", "Bevestigen", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    bool deleted = await DeleteProductAsync("https://pmarcelis.mid-ica.nl/products/?id=" + product.Id);
                    if (deleted)
                    {
                        _products.Remove(product);
                        ProductsDataGrid.Items.Refresh();
                    }
                }
            }
        }
    }
}
