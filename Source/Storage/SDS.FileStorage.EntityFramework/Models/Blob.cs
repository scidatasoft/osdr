using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace Sds.FileStorage.EntityFramework
{
    [Table("Blobs")]
    public class ef_Blob
	{
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity), Key()]
        public int Id { get; set; }

        [Required]
        public byte[] Data { get; set; }
    }
}
