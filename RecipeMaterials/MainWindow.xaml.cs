using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using Newtonsoft.Json;
using RecipeMaterials.Annotations;

namespace RecipeMaterials
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            List<Recipe> recipes = LoadRecipes();

            IngredientList ingredients = new IngredientList(recipes);
            _ingredientList.DataContext = ingredients;
            ingredients.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == "Ingredients")
                    _ingredientList.Items.Refresh();
            };
            foreach (var recipe in recipes)
            {
                var recipeControl = new RecipeControl();
                var model = new RecipeModel(recipe.Name, "/Images/" + recipe.Name + ".png");
                recipeControl.DataContext = model;
                _recipePanel.Children.Add(recipeControl);
            }
            
        }

        private List<Recipe> LoadRecipes()
        {
            List<Recipe> recipes;
            using (var stream = new StreamReader(new FileStream("recipes.json", FileMode.Open)))
            {
                recipes = JsonConvert.DeserializeObject<List<Recipe>>(stream.ReadToEnd());
            }
            return recipes;
        }
    }

    public class Recipe
    {
        public string Name { get; set; }
        public Dictionary<string, int> Materials { get; set; }
    }

    public class Ingredient
    {
        public string Name { get; set; }
        public int Required { get; set; }
        public int Obtained { get; set; }
    }

    public class IngredientList : INotifyPropertyChanged
    {
        public List<Ingredient> Ingredients { get; set; }
        public ICommand LeftClickCommand { get; private set; }
        public ICommand RightClickCommand { get; private set; }

        private void RightClickDelegate(int selectedIndex)
        {
            Ingredients[selectedIndex].Obtained--;
            if (Ingredients[selectedIndex].Obtained < 0)
                Ingredients[selectedIndex].Obtained = 0;
            OnPropertyChanged(nameof(Ingredients));
            Console.WriteLine(Ingredients[selectedIndex].Obtained);
        }

        private void LeftClickDelegate(int selectedIndex)
        {
            Ingredients[selectedIndex].Obtained++;
            OnPropertyChanged(nameof(Ingredients));
            Console.WriteLine(Ingredients[selectedIndex].Obtained);
        }

        public IngredientList(List<Recipe> recipes)
        {
            RightClickCommand = new RelayCommand<int>(RightClickDelegate);
            LeftClickCommand = new RelayCommand<int>(LeftClickDelegate);

            Ingredients = new List<Ingredient>();
            Dictionary<string, int> rawIngredients = new Dictionary<string, int>();
            List<string> craftedFood = new List<string>();
            craftedFood.AddRange(recipes.Select(r=>r.Name));

            Queue<string> toResolve = new Queue<string>();

            foreach (var recipe in recipes)
            {
                foreach (var key in recipe.Materials.Keys)
                {
                    if (!craftedFood.Contains(key))
                    {
                        var i = Ingredients.FirstOrDefault(s => s.Name == key);
                        if (i == null)
                            Ingredients.Add(new Ingredient() {Name = key, Required = recipe.Materials[key]});
                        else
                        {
                            i.Required += recipe.Materials[key];
                        }
                    }
                    else
                    {
                        toResolve.Enqueue(key);
                    }
                }
            }

            while (toResolve.Count > 0)
            {
                var r = toResolve.Dequeue();
                var rec = recipes.Find(f => f.Name == r);
                foreach (var mat in rec.Materials.Keys)
                {
                    if (craftedFood.Contains(mat))
                    {
                        toResolve.Enqueue(mat);
                    }
                    else
                    {
                        Ingredients.Find(f => f.Name == mat).Required += rec.Materials[mat];
                    }
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RecipeModel : INotifyPropertyChanged
    { 
        private bool _completed;
        public bool Completed { get { return _completed; }
            set
            {
                if (_completed != value)
                {
                    _completed = value;
                        OnPropertyChanged();
                }
            }
        }

        private string _name;
        public string Name => _name;

        private string _image;
        public string Image => _image;

        public RecipeModel(string name, string path)
        {
            _name = name;
            _image = path;
        } 

        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
