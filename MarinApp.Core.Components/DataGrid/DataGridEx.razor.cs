using MarinApp.Core.Data;
using MarinApp.Core.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MarinApp.Core.Components.DataGrid
{
    public partial class DataGridEx<TEntity> : MudDataGrid<TEntity>
    {
        private UIEntityMetadata? _metadata;
        private RenderFragment _dataGridFragment;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            _dataGridFragment = BuildDataGrid();
        }

        public RenderFragment BuildDataGrid()
        {
            if (_metadata == null)
                _metadata = UIMetadataExtractor.Get<TEntity>();

            if (_metadata == null)
                throw new InvalidOperationException($"No metadata found for entity type {typeof(TEntity).Name}");

            RenderFragment value = builder =>
                    {
                        int seq = 0;
                        builder.OpenComponent(seq++, typeof(MudDataGrid<TEntity>));
                        builder.AddAttribute(seq++, "Items", typeof(TEntity));
                        builder.AddAttribute(seq++, "ReadOnly", false);
                        //builder.AddAttribute(seq++, "CommittedItemChanges", EventCallback.Factory.Create < ...> (this, OnCommittedItemChanges)); // Replace ... with the actual type

                        builder.AddAttribute(seq++, "Columns", (RenderFragment)(colBuilder =>
                        {
                            int colSeq = 0;
                            foreach (var field in _metadata.Fields)
                            {
                                var propertyColumnType = typeof(PropertyColumn<,>).MakeGenericType(typeof(TEntity), field.FieldType);
                                colBuilder.OpenComponent(colSeq++, propertyColumnType);
                                colBuilder.AddAttribute(colSeq++, "Property", field.Name);
                                colBuilder.AddAttribute(colSeq++, "Sortable", true);
                                colBuilder.AddAttribute(colSeq++, "Filterable", true);
                            }
                            colBuilder.CloseComponent();
                        }));
                        builder.CloseComponent();
                    };
            return value;
        }

        private static Expression<Func<T, object>> GetPropertyExpression<T>(string propertyName)
        {
            var param = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(param, propertyName);
            Expression body = property.Type.IsValueType
                ? Expression.Convert(property, typeof(object))
                : (Expression)property;
            return Expression.Lambda<Func<T, object>>(body, param);
        }
    }
}
