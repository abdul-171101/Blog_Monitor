import React from 'react'

const PageHeader: React.FC<{ title: string; children?: React.ReactNode }> = ({ title, children }) => (
  <div className="flex items-center justify-between mb-6">
    <h1 className="text-2xl font-bold">{title}</h1>
    <div>{children}</div>
  </div>
)

export default PageHeader
