using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using TimeTrackOfGeniuses.Models;

namespace TimeTrackOfGeniuses
{
  public partial class MainWindow : Window
  {
    private ObservableCollection<PersonnageHistorique> personnages;
    private const string FICHIER_SAUVEGARDE = "personnages.dat";
    private const int PIXELS_PAR_ANNEE = 10; // Échelle : 10 pixels = 1 an

    public MainWindow()
    {
      InitializeComponent();
      personnages = new ObservableCollection<PersonnageHistorique>();
      personnages.CollectionChanged += (s, e) => MettreAJourAffichage();

      // Initialiser les contrôles
      dpNaissance.SelectedDate = DateTime.Today;
      dpDeces.SelectedDate = DateTime.Today;

      // Charger les données si elles existent
      ChargerDonnees();

      // Initialiser la ComboBox
      MettreAJourListePersonnages();
    }

    private void BtnAjouter_Click(object sender, RoutedEventArgs e)
    {
      if (string.IsNullOrWhiteSpace(txtNom.Text) || dpNaissance.SelectedDate == null)
      {
        MessageBox.Show("Veuillez remplir tous les champs obligatoires (Nom et Date de naissance).", "Champs manquants", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }

      DateTime dateNaissance = dpNaissance.SelectedDate.Value;
      DateTime? dateDeces = chkVivant.IsChecked == true ? null : dpDeces.SelectedDate;

      if (dateDeces.HasValue && dateDeces.Value < dateNaissance)
      {
        MessageBox.Show("La date de décès ne peut pas être antérieure à la date de naissance.", "Erreur de date", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }

      var personnage = new PersonnageHistorique(
          txtNom.Text.Trim(),
          dateNaissance,
          dateDeces,
          txtDescription.Text
      );

      personnages.Add(personnage);
      ReinitialiserFormulaire();
    }

    private void ChkVivant_Checked(object sender, RoutedEventArgs e)
    {
      dpDeces.IsEnabled = false;
      dpDeces.SelectedDate = null;
    }

    private void ChkVivant_Unchecked(object sender, RoutedEventArgs e)
    {
      dpDeces.IsEnabled = true;
      dpDeces.SelectedDate = DateTime.Today;
    }

    private void ReinitialiserFormulaire()
    {
      txtNom.Clear();
      dpNaissance.SelectedDate = DateTime.Today;
      dpDeces.SelectedDate = DateTime.Today;
      chkVivant.IsChecked = false;
      txtDescription.Clear();
      txtNom.Focus();
    }

    private void MettreAJourAffichage()
    {
      lblNbPersonnages.Text = personnages.Count.ToString();
      MettreAJourListePersonnages();
      DessinerLigneDuTemps();
    }

    private void DessinerLigneDuTemps()
    {
      timelineCanvas.Children.Clear();

      if (!personnages.Any()) return;

      // Trier les personnages par date de naissance
      var personnagesTries = personnages.OrderBy(p => p.DateNaissance).ToList();
      int anneeMin = personnages.Min(p => p.DateNaissance.Year) - 5;
      int anneeMax = Math.Max(DateTime.Now.Year, personnages.Max(p => p.DateMort?.Year ?? p.DateNaissance.Year)) + 5;

      // Ajuster la largeur du canvas
      timelineCanvas.Width = 100 + (anneeMax - anneeMin) * PIXELS_PAR_ANNEE;

      // Dessiner la ligne de temps principale
      Line ligneTemps = new Line
      {
        X1 = 50,
        Y1 = 100,
        X2 = 50 + (anneeMax - anneeMin) * PIXELS_PAR_ANNEE,
        Y2 = 100,
        Stroke = Brushes.Black,
        StrokeThickness = 2,
        StrokeDashArray = new DoubleCollection(new double[] { 5, 5 })
      };
      timelineCanvas.Children.Add(ligneTemps);

      // Liste pour suivre les positions Y utilisées
      List<double> yPositions = new List<double>();

      // Dessiner les marqueurs d'année
      for (int annee = anneeMin; annee <= anneeMax; annee += 10)
      {
        double x = 50 + (annee - anneeMin) * PIXELS_PAR_ANNEE;

        // Marqueur d'année
        Line marqueur = new Line
        {
          X1 = x,
          Y1 = 95,
          Y2 = 105,
          Stroke = Brushes.Black,
          StrokeThickness = 1
        };
        timelineCanvas.Children.Add(marqueur);

        // Étiquette d'année
        TextBlock txtAnnee = new TextBlock
        {
          Text = annee.ToString(),
          Margin = new Thickness(x - 15, 70, 0, 0),
          FontSize = 10
        };
        timelineCanvas.Children.Add(txtAnnee);
      }

      // Couleurs aléatoires pour chaque personnage
      Random rnd = new Random();

      // Dessiner les personnages
      foreach (var personnage in personnagesTries)
      {
        double xNaissance = 50 + (personnage.DateNaissance.Year - anneeMin) * PIXELS_PAR_ANNEE;
        double xMort = personnage.DateMort.HasValue
            ? 50 + (personnage.DateMort.Value.Year - anneeMin) * PIXELS_PAR_ANNEE
            : 50 + (DateTime.Now.Year - anneeMin) * PIXELS_PAR_ANNEE;

        // Vérifier que les positions X sont valides
        if (xNaissance < 50 || xMort < 50) continue;
        if (xNaissance > timelineCanvas.Width && xMort > timelineCanvas.Width) continue;

        // Trouver une position Y disponible
        double y = 120; // Position de départ au-dessus de la ligne
        bool positionTrouvee = false;

        while (!positionTrouvee)
        {
          bool chevauchement = false;
          foreach (var yPos in yPositions)
          {
            if (Math.Abs(y - yPos) < 25) // 25 pixels d'espacement minimum
            {
              chevauchement = true;
              break;
            }
          }

          if (!chevauchement)
          {
            positionTrouvee = true;
            yPositions.Add(y);
          }
          else
          {
            y += 25; // Espacement entre les lignes
          }
        }

        // Générer une couleur aléatoire pour ce personnage
        Color couleur = Color.FromRgb(
            (byte)rnd.Next(50, 200),
            (byte)rnd.Next(50, 200),
            (byte)rnd.Next(50, 200)
        );

        // Ligne de vie (entre naissance et mort)
        if (personnage.DateMort.HasValue || xNaissance != xMort)
        {
          Line ligneVie = new Line
          {
            X1 = xNaissance,
            Y1 = y - 10,
            X2 = xMort,
            Y2 = y - 10,
            Stroke = new SolidColorBrush(couleur),
            StrokeThickness = 3,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
          };
          timelineCanvas.Children.Add(ligneVie);
        }

        // Ligne pointillée à la naissance
        Line ligneNaissance = new Line
        {
          X1 = xNaissance,
          Y1 = 95,
          X2 = xNaissance,
          Y2 = y - 10,
          Stroke = new SolidColorBrush(couleur),
          StrokeThickness = 1,
          StrokeDashArray = new DoubleCollection(new double[] { 2, 2 })
        };
        timelineCanvas.Children.Add(ligneNaissance);

        // Ligne pointillée à la mort (si décédé)
        if (personnage.DateMort.HasValue)
        {
          Line ligneMort = new Line
          {
            X1 = xMort,
            Y1 = 95,
            X2 = xMort,
            Y2 = y - 10,
            Stroke = new SolidColorBrush(couleur),
            StrokeThickness = 1,
            StrokeDashArray = new DoubleCollection(new double[] { 2, 2 })
          };
          timelineCanvas.Children.Add(ligneMort);
        }

        // Nom du personnage
        TextBlock txtNom = new TextBlock
        {
          Text = personnage.Nom,
          Margin = new Thickness(xNaissance + 5, y - 30, 0, 0),
          FontWeight = FontWeights.Bold,
          Foreground = new SolidColorBrush(Colors.Black),
          ToolTip = $"{personnage.Nom}\n" +
                     $"Né le: {personnage.DateNaissance:dd/MM/yyyy}\n" +
                     $"Décédé le: {(personnage.DateMort.HasValue ? personnage.DateMort.Value.ToString("dd/MM/yyyy") : "Toujours vivant")}\n" +
                     $"{personnage.Description}"
        };
        timelineCanvas.Children.Add(txtNom);
      }

      // Ajuster la hauteur du canvas en fonction du contenu
      timelineCanvas.Height = yPositions.Any() ? yPositions.Max() + 30 : 200;
    }

    private void AjouterPersonnageSurLigneDuTemps(PersonnageHistorique personnage, int anneeMin)
    {
      double xNaissance = 50 + (personnage.DateNaissance.Year - anneeMin) * PIXELS_PAR_ANNEE;
      double xFin = personnage.DateMort.HasValue
          ? 50 + (personnage.DateMort.Value.Year - anneeMin) * PIXELS_PAR_ANNEE
          : 50 + (DateTime.Now.Year - anneeMin) * PIXELS_PAR_ANNEE;

      // Ligne de vie
      Line ligneVie = new Line
      {
        X1 = xNaissance,
        Y1 = 150,
        X2 = xFin,
        Y2 = 150,
        Stroke = Brushes.Blue,
        StrokeThickness = 2,
        ToolTip = $"{personnage.Nom}\n({personnage.DateNaissance.Year} - {(personnage.DateMort.HasValue ? personnage.DateMort.Value.Year.ToString() : "présent")})"
      };

      timelineCanvas.Children.Add(ligneVie);

      // Point de naissance
      Ellipse pointNaissance = new Ellipse
      {
        Width = 10,
        Height = 10,
        Fill = Brushes.Green,
        Stroke = Brushes.DarkGreen,
        StrokeThickness = 1,
        ToolTip = $"Naissance: {personnage.Nom} ({personnage.DateNaissance:d})"
      };

      Canvas.SetLeft(pointNaissance, xNaissance - 5);
      Canvas.SetTop(pointNaissance, 145);
      timelineCanvas.Children.Add(pointNaissance);

      // Point de décès (si applicable)
      if (personnage.DateMort.HasValue)
      {
        Ellipse pointDeces = new Ellipse
        {
          Width = 10,
          Height = 10,
          Fill = Brushes.Red,
          Stroke = Brushes.DarkRed,
          StrokeThickness = 1,
          ToolTip = $"Décès: {personnage.Nom} ({personnage.DateMort.Value:d})"
        };

        Canvas.SetLeft(pointDeces, xFin - 5);
        Canvas.SetTop(pointDeces, 145);
        timelineCanvas.Children.Add(pointDeces);
      }

      // Étiquette du nom
      TextBlock etiquette = new TextBlock
      {
        Text = personnage.Nom,
        Background = Brushes.White,
        Padding = new Thickness(3),
        TextWrapping = System.Windows.TextWrapping.Wrap,
        MaxWidth = 150,
        ToolTip = personnage.Description
      };

      // Positionner l'étiquette au-dessus ou en dessous de la ligne pour éviter les chevauchements
      double top = personnages.IndexOf(personnage) % 2 == 0 ? 120 : 170;
      Canvas.SetLeft(etiquette, xNaissance);
      Canvas.SetTop(etiquette, top);
      timelineCanvas.Children.Add(etiquette);
    }

    private void BtnSauvegarder_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        using (FileStream fs = new FileStream(FICHIER_SAUVEGARDE, FileMode.Create))
        {
          BinaryFormatter formatter = new BinaryFormatter();
          formatter.Serialize(fs, personnages.ToList());
        }
        MessageBox.Show("Données sauvegardées avec succès.", "Sauvegarde réussie", MessageBoxButton.OK, MessageBoxImage.Information);
      }
      catch (Exception exception)
      {
        MessageBox.Show($"Erreur lors de la sauvegarde : {exception.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void BtnCharger_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        if (File.Exists(FICHIER_SAUVEGARDE))
        {
          using (FileStream fs = new FileStream(FICHIER_SAUVEGARDE, FileMode.Open))
          {
            BinaryFormatter formatter = new BinaryFormatter();
            var liste = (List<PersonnageHistorique>)formatter.Deserialize(fs);
            personnages.Clear();
            foreach (var item in liste)
            {
              personnages.Add(item);
            }
          }
        }
        else
        {
          MessageBox.Show("Aucune donnée sauvegardée trouvée.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }
      }
      catch (Exception exception)
      {
        MessageBox.Show($"Erreur lors du chargement : {exception.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void ChargerDonnees()
    {
      if (File.Exists(FICHIER_SAUVEGARDE))
      {
        BtnCharger_Click(null, null);
      }
    }

    private void BtnExporterCSV_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        var saveFileDialog = new Microsoft.Win32.SaveFileDialog
        {
          FileName = "personnages",
          DefaultExt = ".csv",
          Filter = "Fichiers CSV (*.csv)|*.csv"
        };

        if (saveFileDialog.ShowDialog() == true)
        {
          var csv = new StringBuilder();
          // En-tête
          csv.AppendLine("Nom;DateNaissance;DateMort;Description");

          // Données
          foreach (var p in personnages)
          {
            string dateMort = p.DateMort?.ToString("yyyy-MM-dd") ?? "";
            string description = p.Description?.Replace(";", ",") ?? "";
            csv.AppendLine($"\"{p.Nom}\";{p.DateNaissance:yyyy-MM-dd};{dateMort};\"{description}\"");
          }

          File.WriteAllText(saveFileDialog.FileName, csv.ToString(), Encoding.UTF8);
          MessageBox.Show("Export CSV réussi !", "Succès",
              MessageBoxButton.OK, MessageBoxImage.Information);
        }
      }
      catch (Exception exception)
      {
        MessageBox.Show($"Erreur lors de l'export CSV : {exception.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void BtnImporterCSV_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
          DefaultExt = ".csv",
          Filter = "Fichiers CSV (*.csv)|*.csv"
        };

        if (openFileDialog.ShowDialog() == true)
        {
          var newPersonnages = new List<PersonnageHistorique>();
          var lines = File.ReadAllLines(openFileDialog.FileName, Encoding.UTF8);
          int nbPersonnagesAjoutes = 0;

          // Sauter l'en-tête
          foreach (var line in lines.Skip(1))
          {
            var parts = ParseCsvLine(line);
            if (parts.Length >= 3)
            {
              try
              {
                string nom = parts[0].Trim('"');
                DateTime naissance = DateTime.Parse(parts[1]);
                DateTime? deces = string.IsNullOrEmpty(parts[2]) ? null : (DateTime?)DateTime.Parse(parts[2]);
                string description = parts.Length > 3 ? parts[3].Trim('"') : "";

                // Vérifier si le personnage existe déjà
                if (!personnages.Any(p => p.Nom == nom && p.DateNaissance == naissance))
                {
                  personnages.Add(new PersonnageHistorique(nom, naissance, deces, description));
                  nbPersonnagesAjoutes++;
                }
              }
              catch (Exception exception)
              {
                MessageBox.Show($"Erreur lors de la lecture de la ligne : {line}\n\n{exception.Message}", "Erreur de format", MessageBoxButton.OK, MessageBoxImage.Warning);
              }
            }
          }

          MessageBox.Show($"{nbPersonnagesAjoutes} nouveaux personnages importés avec succès !\n" +
                        $"{lines.Length - 1 - nbPersonnagesAjoutes} doublons ignorés.",
                        "Importation réussie", MessageBoxButton.OK, MessageBoxImage.Information);
        }
      }
      catch (Exception exception)
      {
        MessageBox.Show($"Erreur lors de l'import CSV : {exception.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private string[] ParseCsvLine(string line)
    {
      var result = new List<string>();
      bool inQuotes = false;
      var currentValue = new StringBuilder();

      for (int i = 0; i < line.Length; i++)
      {
        char c = line[i];

        if (c == '"')
        {
          inQuotes = !inQuotes;
          continue;
        }

        if (c == ';' && !inQuotes)
        {
          result.Add(currentValue.ToString());
          currentValue.Clear();
          continue;
        }

        currentValue.Append(c);
      }

      // Ajouter la dernière valeur
      result.Add(currentValue.ToString());
      return result.ToArray();
    }

    private void MettreAJourListePersonnages()
    {
      // Sauvegarder la sélection actuelle
      var selected = cbPersonnages.SelectedItem as PersonnageHistorique;

      // Mettre à jour la source de la ComboBox
      if (cbPersonnages.ItemsSource == null)
      {
        cbPersonnages.ItemsSource = personnages;
      }
      else
      {
        // Utiliser un ICollectionView pour rafraîchir la vue sans réinitialiser la source
        var view = CollectionViewSource.GetDefaultView(cbPersonnages.ItemsSource);
        view.Refresh();
      }

      // Restaurer la sélection si possible
      if (selected != null)
      {
        var existingItem = personnages.FirstOrDefault(p =>
            p.Nom == selected.Nom &&
            p.DateNaissance == selected.DateNaissance);

        if (existingItem != null)
        {
          cbPersonnages.SelectedItem = existingItem;
        }
      }
    }

    private void CbPersonnages_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (e.AddedItems.Count > 0 && e.AddedItems[0] is PersonnageHistorique personnage)
      {
        // Faire défiler jusqu'au personnage sélectionné
        FaireDefilerVersPersonnage(personnage);
      }
    }

    private void FaireDefilerVersPersonnage(PersonnageHistorique personnage)
    {
      if (personnage == null || !personnages.Any()) return;

      try
      {
        // Calculer la position X du personnage sur la timeline
        int anneeMin = personnages.Min(p => p.DateNaissance.Year) - 5;
        double xPosition = 50 + (personnage.DateNaissance.Year - anneeMin) * PIXELS_PAR_ANNEE;

        // Trouver le ScrollViewer parent du timelineCanvas
        var scrollViewer = FindVisualParent<ScrollViewer>(timelineCanvas);
        if (scrollViewer == null)
        {
          // Si on ne trouve pas de ScrollViewer, essayer de le trouver dans l'arborescence visuelle
          scrollViewer = FindVisualChild<ScrollViewer>(this);
          if (scrollViewer == null) return;
        }

        // Calculer la position de défilement pour centrer le personnage
        double scrollPosition = xPosition - (scrollViewer.ViewportWidth / 2);
        scrollViewer.ScrollToHorizontalOffset(Math.Max(0, scrollPosition));
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Erreur lors du défilement : {ex.Message}");
      }
    }

    // Méthode utilitaire pour trouver un parent d'un type spécifique
    private static T GetVisualParent<T>(DependencyObject child) where T : DependencyObject
    {
      while (child != null && !(child is T))
      {
        child = VisualTreeHelper.GetParent(child);
      }
      return child as T;
    }

    private static T FindVisualChild<T>(DependencyObject depObj) where T : DependencyObject
    {
      if (depObj == null) return null;
      var counter = VisualTreeHelper.GetChildrenCount(depObj);
      for (int i = 0; i < counter; i++)
      {
        var child = VisualTreeHelper.GetChild(depObj, i);
        if (child is T result) return result;

        var childItem = FindVisualChild<T>(child);
        if (childItem != null) return childItem;
      }

      return null;
    }

    private static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
    {
      var parentObject = VisualTreeHelper.GetParent(child);
      if (parentObject == null) return null;

      if (parentObject is T parent) return parent;

      return FindVisualParent<T>(parentObject);
    }
  }
}