import React, { useState, useEffect } from 'react';
import { propertyService, PropertyGroupResponseDto, PropertyResponseDto } from '../../services/propertyService';

const PropertyHubHome: React.FC = () => {
  const [propertyGroups, setPropertyGroups] = useState<PropertyGroupResponseDto[]>([]);
  const [properties, setProperties] = useState<PropertyResponseDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      setError(null);
      const [groupsData, propertiesData] = await Promise.all([
        propertyService.getPropertyGroups(),
        propertyService.getProperties(),
      ]);
      setPropertyGroups(groupsData);
      setProperties(propertiesData);
    } catch (err: any) {
      setError(err.message || 'Failed to load property data');
    } finally {
      setLoading(false);
    }
  };

  // Group properties by property group
  const propertiesByGroup = propertyGroups.map(group => ({
    group,
    properties: properties.filter(p => p.propertyGroupId === group.propertyGroupId),
  }));

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <div className="text-gray-600 text-lg">Loading property data...</div>
        </div>
      </div>
    );
  }

  return (
    <div>
        <div className="mb-6">
          <h1 className="text-3xl font-bold text-gray-900">Property Hub</h1>
          <p className="mt-2 text-gray-600">View property groups and properties you have access to</p>
        </div>

        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded mb-6">
            {error}
          </div>
        )}

        {propertyGroups.length === 0 && properties.length === 0 && !error && (
          <div className="bg-white rounded-lg shadow p-8 text-center">
            <p className="text-gray-500">No property groups or properties found.</p>
          </div>
        )}

        {/* Property Groups Section */}
        {propertiesByGroup.length > 0 && (
          <div className="space-y-6">
            {propertiesByGroup.map(({ group, properties: groupProperties }) => (
              <div key={group.propertyGroupId} className="bg-white rounded-lg shadow">
                <div className="px-6 py-4 border-b border-gray-200">
                  <div className="flex justify-between items-center">
                    <div>
                      <h2 className="text-xl font-semibold text-gray-900">
                        {group.propertyGroupName}
                      </h2>
                      {group.description && (
                        <p className="mt-1 text-sm text-gray-500">{group.description}</p>
                      )}
                    </div>
                    <div className="text-sm text-gray-500">
                      {groupProperties.length} {groupProperties.length === 1 ? 'property' : 'properties'}
                    </div>
                  </div>
                </div>

                {/* Properties in this group */}
                {groupProperties.length > 0 ? (
                  <div className="px-6 py-4">
                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                      {groupProperties.map((property) => (
                        <div
                          key={property.propertyId}
                          className="border border-gray-200 rounded-lg p-4 hover:shadow-md transition-shadow cursor-pointer"
                          onClick={() => {
                            // Placeholder for property details navigation
                            console.log('Navigate to property details:', property.propertyId);
                          }}
                        >
                          <h3 className="font-medium text-gray-900 mb-2">
                            {property.propertyName}
                          </h3>
                          {property.address && (
                            <p className="text-sm text-gray-600 mb-1">{property.address}</p>
                          )}
                          {property.postCode && (
                            <p className="text-sm text-gray-500">{property.postCode}</p>
                          )}
                          <div className="mt-4 flex space-x-2">
                            <button
                              className="text-xs text-blue-600 hover:text-blue-800"
                              onClick={(e) => {
                                e.stopPropagation();
                                // Placeholder for journal logs navigation
                                console.log('Navigate to journal logs for property:', property.propertyId);
                              }}
                            >
                              Journal Logs
                            </button>
                            <span className="text-gray-300">|</span>
                            <button
                              className="text-xs text-blue-600 hover:text-blue-800"
                              onClick={(e) => {
                                e.stopPropagation();
                                // Placeholder for contact logs navigation
                                console.log('Navigate to contact logs for property:', property.propertyId);
                              }}
                            >
                              Contact Logs
                            </button>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                ) : (
                  <div className="px-6 py-8 text-center text-gray-500">
                    No properties in this group
                  </div>
                )}
              </div>
            ))}
          </div>
        )}

        {/* Unassigned Properties (if any) */}
        {properties.filter(p => !propertyGroups.some(g => g.propertyGroupId === p.propertyGroupId)).length > 0 && (
          <div className="mt-6 bg-white rounded-lg shadow">
            <div className="px-6 py-4 border-b border-gray-200">
              <h2 className="text-xl font-semibold text-gray-900">Unassigned Properties</h2>
            </div>
            <div className="px-6 py-4">
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                {properties
                  .filter(p => !propertyGroups.some(g => g.propertyGroupId === p.propertyGroupId))
                  .map((property) => (
                    <div
                      key={property.propertyId}
                      className="border border-gray-200 rounded-lg p-4 hover:shadow-md transition-shadow cursor-pointer"
                    >
                      <h3 className="font-medium text-gray-900 mb-2">{property.propertyName}</h3>
                      {property.address && (
                        <p className="text-sm text-gray-600 mb-1">{property.address}</p>
                      )}
                      {property.postCode && (
                        <p className="text-sm text-gray-500">{property.postCode}</p>
                      )}
                    </div>
                  ))}
              </div>
            </div>
          </div>
        )}
    </div>
  );
};

export default PropertyHubHome;
