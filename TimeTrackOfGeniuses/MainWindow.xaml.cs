using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Windows;
using System.Windows.Controls;
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
        }

        private void BtnAjouter_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNom.Text) || dpNaissance.SelectedDate == null)
            {
                MessageBox.Show("Veuillez remplir tous les champs obligatoires (Nom et Date de naissance).", 
                    "Champs manquants", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime dateNaissance = dpNaissance.SelectedDate.Value;
            DateTime? dateDeces = chkVivant.IsChecked == true ? null : dpDeces.SelectedDate;

            if (dateDeces.HasValue && dateDeces.Value < dateNaissance)
            {
                MessageBox.Show("La date de décès ne peut pas être antérieure à la date de naissance.", 
                    "Erreur de date", MessageBoxButton.OK, MessageBoxImage.Error);
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
            DessinerLigneDuTemps();
        }

        private void DessinerLigneDuTemps()
        {
            timelineCanvas.Children.Clear();
            
            if (!personnages.Any()) return;

            // Trouver la plage de dates
            int anneeMin = personnages.Min(p => p.DateNaissance.Year) - 5;
            int anneeMax = DateTime.Now.Year + 5;
            
            // Dessiner la ligne de temps
            Line timeline = new Line
            {
                X1 = 50,
                Y1 = 150,
                X2 = 50 + (anneeMax - anneeMin) * PIXELS_PAR_ANNEE,
                Y2 = 150,
                Stroke = Brushes.Black,
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection() { 5, 5 }
            };
            timelineCanvas.Children.Add(timeline);

            // Ajouter les marqueurs d'année
            for (int annee = anneeMin; annee <= anneeMax; annee += 10)
            {
                double x = 50 + (annee - anneeMin) * PIXELS_PAR_ANNEE;
                
                // Ligne de repère
                Line marqueur = new Line
                {
                    X1 = x,
                    Y1 = 145,
                    X2 = x,
                    Y2 = 155,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };
                timelineCanvas.Children.Add(marqueur);

                // Étiquette de l'année
                TextBlock texteAnnee = new TextBlock
                {
                    Text = annee.ToString(),
                    Margin = new Thickness(x - 15, 160, 0, 0)
                };
                timelineCanvas.Children.Add(texteAnnee);
            }

            // Ajouter les personnages
            foreach (var personnage in personnages)
            {
                AjouterPersonnageSurLigneDuTemps(personnage, anneeMin);
            }

            // Ajuster la taille du canvas
            timelineCanvas.Width = 50 + (anneeMax - anneeMin) * PIXELS_PAR_ANNEE + 50;
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
                MessageBox.Show("Données sauvegardées avec succès.", "Sauvegarde réussie", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la sauvegarde : {ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    MessageBox.Show("Données chargées avec succès.", "Chargement réussi", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Aucune donnée sauvegardée trouvée.", "Information", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement : {ex.Message}", 
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChargerDonnees()
        {
            if (File.Exists(FICHIER_SAUVEGARDE))
            {
                BtnCharger_Click(null, null);
            }
        }
    }
}