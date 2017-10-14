using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    [Table(name: "User")]
    public partial class User
    {
        [Key(), Column("UserId", Order = 10)]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId
        {
            get { return _UserId; }
            set { _UserId = value; }
        }
        [ScaffoldColumn(false)]
        private int _UserId;

        [ScaffoldColumn(false)]
        private string _Username;
        [Column("Username", Order = 20), Required(ErrorMessage = "A Username is required.")]
        [DisplayFormat(NullDisplayText = "Enter Username")]
        [MaxLength(20)]
        [StringLength(maximumLength: 20, MinimumLength = 2, ErrorMessage = "Username cannot be longer than 20 characters and must be at least 2 characters.")]
        public string Username
        {
            get { return _Username; }
            set { _Username = value; }
        }

        [Column(Order = 30), ForeignKey("User")]
        public int AuthorId { get; set; }
        public virtual User Author { get; set; }

        [DefaultValue(1)]
        [Column("Version", Order = 40)]
        public Nullable<int> Version { get; set; }

        [DataType(DataType.DateTime)]
        [Column(Order = 50, TypeName = "DateTime")]
        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy HH:mm:ss}", ApplyFormatInEditMode = true)]
        public DateTime? VersionDate { get; set; } = DateTime.Now;

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string pPropertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(pPropertyName));
            }
        }
        
        // Create a copy of an accomplishment to save.
        // If your object is databound, this copy is not databound.
        public User GetCopy()
        {
            return (User)this.MemberwiseClone();
        }


    }
}
