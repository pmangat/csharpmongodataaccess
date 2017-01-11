using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MongoDb.DataAccess.Exceptions
{
    public class ValidationException : ApplicationException
    {
        public ValidationException(List<ValidationResult> results)
        {
            Errors = results;
        }

        public List<ValidationResult> Errors { get; private set; }
    }
}