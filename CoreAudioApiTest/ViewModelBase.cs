using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;

namespace CoreAudioApiTest
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        protected ViewModelBase() { }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void RaisePropertyChanged(params string[] names)
        {
            var h = PropertyChanged;
            if (h == null) return;
            CheckPropertyName(names);
            foreach (var name in names)
            {
                h(this, new PropertyChangedEventArgs(name));
            }
        }

        [Conditional("DEBUG")]
        private void CheckPropertyName(params string[] names)
        {
            var props = GetType().GetProperties();
            foreach (var name in names)
            {
                var prop = props.Where(p => p.Name == name).SingleOrDefault();
                if (prop == null) throw new ArgumentException(name);
            }
        }

        protected void RaisePropertyChanged<T>(params Expression<Func<T>>[] propertyExpression)
        {
            RaisePropertyChanged(propertyExpression.Select(ex => ((MemberExpression)ex.Body).Member.Name).ToArray());
        }
    }
}
