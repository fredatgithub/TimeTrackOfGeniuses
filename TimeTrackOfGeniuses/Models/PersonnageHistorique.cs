// Fichier: Models/PersonnageHistorique.cs
using System;

namespace TimeTrackOfGeniuses.Models
{
  [Serializable]
  public class PersonnageHistorique
  {
    public string Nom { get; set; }
    public DateTime DateNaissance { get; set; }
    public DateTime? DateMort { get; set; } // Nullable pour les personnages toujours vivants
    public string Description { get; set; }

    public PersonnageHistorique()
    {
      // Constructeur par défaut nécessaire pour la sérialisation
    }

    public PersonnageHistorique(string nom, DateTime dateNaissance, DateTime? dateMort, string description)
    {
      Nom = nom;
      DateNaissance = dateNaissance;
      DateMort = dateMort;
      Description = description;
    }

    public int AgeAuDeces
    {
      get
      {
        if (DateMort.HasValue)
        {
          int age = DateMort.Value.Year - DateNaissance.Year;
          if (DateNaissance.Date > DateMort.Value.AddYears(-age)) age--;
          return age;
        }
        return -1; // Si la date de mort n'est pas définie
      }
    }

    public override string ToString()
    {
      return $"{Nom} ({DateNaissance.Year} - {(DateMort.HasValue ? DateMort.Value.Year.ToString() : "présent")})";
    }
  }
}