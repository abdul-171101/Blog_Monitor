import React from 'react'

type Column<T> = { header: string; accessor: (row: T) => React.ReactNode }

function DataTable<T>({ columns, rows, emptyMessage = 'No data' }: { columns: Column<T>[]; rows: T[]; emptyMessage?: string }) {
  return (
    <div className="overflow-x-auto bg-white rounded shadow">
      <table className="min-w-full table-auto">
        <thead className="bg-gray-50">
          <tr>
            {columns.map((c, i) => (
              <th key={i} className="text-left px-4 py-2 text-sm font-medium text-gray-600">
                {c.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.length === 0 ? (
            <tr>
              <td className="px-4 py-6 text-center text-gray-500" colSpan={columns.length}>
                {emptyMessage}
              </td>
            </tr>
          ) : (
            rows.map((r, ri) => (
              <tr key={ri} className={ri % 2 === 0 ? 'bg-white' : 'bg-gray-50'}>
                {columns.map((c, ci) => (
                  <td key={ci} className="px-4 py-3 text-sm text-gray-700">
                    {c.accessor(r)}
                  </td>
                ))}
              </tr>
            ))
          )}
        </tbody>
      </table>
    </div>
  )
}

export default DataTable
