import React from 'react'

const sizes: Record<string, string> = {
  small: 'w-4 h-4',
  medium: 'w-6 h-6',
  large: 'w-10 h-10',
}

const LoadingSpinner: React.FC<{ size?: 'small' | 'medium' | 'large' }> = ({ size = 'medium' }) => (
  <div className={`border-2 border-t-transparent rounded-full animate-spin ${sizes[size]} border-gray-300`} />
)

export default LoadingSpinner
