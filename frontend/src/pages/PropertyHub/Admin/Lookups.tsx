import React, { useState, useEffect } from 'react';
import { propertyAdminService, JournalTypeDto, ContactLogTypeDto, TagTypeResponseDto, CreateTagTypeRequest, UpdateTagTypeRequest, CreateJournalTypeRequest, UpdateJournalTypeRequest, CreateContactLogTypeRequest, UpdateContactLogTypeRequest, CreateJournalSubTypeRequest, UpdateJournalSubTypeRequest, JournalSubTypeDto } from '../../../services/propertyAdminService';

const Lookups: React.FC = () => {
  const [journalTypes, setJournalTypes] = useState<JournalTypeDto[]>([]);
  const [contactLogTypes, setContactLogTypes] = useState<ContactLogTypeDto[]>([]);
  const [tagTypes, setTagTypes] = useState<TagTypeResponseDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<'journal' | 'contact' | 'tag'>('journal');
  const [showJournalModal, setShowJournalModal] = useState(false);
  const [editingJournalType, setEditingJournalType] = useState<JournalTypeDto | null>(null);
  const [showJournalSubTypeModal, setShowJournalSubTypeModal] = useState(false);
  const [selectedJournalTypeForSubType, setSelectedJournalTypeForSubType] = useState<number | null>(null);
  const [editingJournalSubType, setEditingJournalSubType] = useState<{ subType: JournalSubTypeDto; journalTypeId: number } | null>(null);
  const [showContactModal, setShowContactModal] = useState(false);
  const [editingContactLogType, setEditingContactLogType] = useState<ContactLogTypeDto | null>(null);
  const [showTagModal, setShowTagModal] = useState(false);
  const [editingTagType, setEditingTagType] = useState<TagTypeResponseDto | null>(null);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      setError(null);
      const [journalData, contactData, tagData] = await Promise.all([
        propertyAdminService.getJournalTypes(),
        propertyAdminService.getContactLogTypes(),
        propertyAdminService.getTagTypes(),
      ]);
      setJournalTypes(journalData);
      setContactLogTypes(contactData);
      setTagTypes(tagData);
    } catch (err: any) {
      setError(err.message || 'Failed to load lookup data');
    } finally {
      setLoading(false);
    }
  };

  const handleCreateTagType = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    const request: CreateTagTypeRequest = {
      tagTypeName: formData.get('tagTypeName') as string,
      color: formData.get('color') as string || undefined,
      description: formData.get('description') as string || undefined,
      isActive: (() => {
        const value = formData.get('isActive') as string;
        return value === '' ? undefined : value === 'true';
      })(),
    };

    try {
      await propertyAdminService.createTagType(request);
      setShowTagModal(false);
      loadData();
    } catch (err: any) {
      setError(err.message || 'Failed to create tag type');
    }
  };

  const handleUpdateTagType = async (id: number, e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    const request: UpdateTagTypeRequest = {
      tagTypeName: formData.get('tagTypeName') as string || undefined,
      color: formData.get('color') as string || undefined,
      description: formData.get('description') as string || undefined,
      isActive: (() => {
        const value = formData.get('isActive') as string;
        return value === '' ? undefined : value === 'true';
      })(),
    };

    try {
      await propertyAdminService.updateTagType(id, request);
      setEditingTagType(null);
      loadData();
    } catch (err: any) {
      setError(err.message || 'Failed to update tag type');
    }
  };

  const handleDeleteTagType = async (id: number) => {
    if (!window.confirm('Are you sure you want to delete this tag type?')) return;

    try {
      await propertyAdminService.deleteTagType(id);
      loadData();
    } catch (err: any) {
      setError(err.message || 'Failed to delete tag type');
    }
  };

  const handleCreateJournalType = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    const request: CreateJournalTypeRequest = {
      journalTypeName: formData.get('journalTypeName') as string,
      description: formData.get('description') as string || undefined,
      isActive: (() => {
        const value = formData.get('isActive') as string;
        return value === '' ? undefined : value === 'true';
      })(),
    };

    try {
      await propertyAdminService.createJournalType(request);
      setShowJournalModal(false);
      loadData();
    } catch (err: any) {
      setError(err.message || 'Failed to create journal type');
    }
  };

  const handleUpdateJournalType = async (id: number, e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    const request: UpdateJournalTypeRequest = {
      journalTypeName: formData.get('journalTypeName') as string || undefined,
      description: formData.get('description') as string || undefined,
      isActive: (() => {
        const value = formData.get('isActive') as string;
        return value === '' ? undefined : value === 'true';
      })(),
    };

    try {
      await propertyAdminService.updateJournalType(id, request);
      setEditingJournalType(null);
      loadData();
    } catch (err: any) {
      setError(err.message || 'Failed to update journal type');
    }
  };

  const handleDeleteJournalType = async (id: number) => {
    if (!window.confirm('Are you sure you want to delete this journal type?')) return;

    try {
      await propertyAdminService.deleteJournalType(id);
      loadData();
    } catch (err: any) {
      setError(err.message || 'Failed to delete journal type');
    }
  };

  const handleCreateContactLogType = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    const request: CreateContactLogTypeRequest = {
      contactLogTypeName: formData.get('contactLogTypeName') as string,
      description: formData.get('description') as string || undefined,
      isActive: (() => {
        const value = formData.get('isActive') as string;
        return value === '' ? undefined : value === 'true';
      })(),
    };

    try {
      await propertyAdminService.createContactLogType(request);
      setShowContactModal(false);
      loadData();
    } catch (err: any) {
      setError(err.message || 'Failed to create contact log type');
    }
  };

  const handleUpdateContactLogType = async (id: number, e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    const request: UpdateContactLogTypeRequest = {
      contactLogTypeName: formData.get('contactLogTypeName') as string || undefined,
      description: formData.get('description') as string || undefined,
      isActive: (() => {
        const value = formData.get('isActive') as string;
        return value === '' ? undefined : value === 'true';
      })(),
    };

    try {
      await propertyAdminService.updateContactLogType(id, request);
      setEditingContactLogType(null);
      loadData();
    } catch (err: any) {
      setError(err.message || 'Failed to update contact log type');
    }
  };

  const handleDeleteContactLogType = async (id: number) => {
    if (!window.confirm('Are you sure you want to delete this contact log type?')) return;

    try {
      await propertyAdminService.deleteContactLogType(id);
      loadData();
    } catch (err: any) {
      setError(err.message || 'Failed to delete contact log type');
    }
  };

  const handleCreateJournalSubType = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!selectedJournalTypeForSubType) return;

    const formData = new FormData(e.currentTarget);
    const request: CreateJournalSubTypeRequest = {
      journalTypeId: selectedJournalTypeForSubType,
      journalSubTypeName: formData.get('journalSubTypeName') as string,
      description: formData.get('description') as string || undefined,
      isActive: (() => {
        const value = formData.get('isActive') as string;
        return value === '' ? undefined : value === 'true';
      })(),
    };

    try {
      await propertyAdminService.createJournalSubType(request);
      setShowJournalSubTypeModal(false);
      setSelectedJournalTypeForSubType(null);
      loadData();
    } catch (err: any) {
      setError(err.message || 'Failed to create journal sub type');
    }
  };

  const handleUpdateJournalSubType = async (id: number, e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const formData = new FormData(e.currentTarget);
    const request: UpdateJournalSubTypeRequest = {
      journalSubTypeName: formData.get('journalSubTypeName') as string || undefined,
      description: formData.get('description') as string || undefined,
      isActive: (() => {
        const value = formData.get('isActive') as string;
        return value === '' ? undefined : value === 'true';
      })(),
    };

    try {
      await propertyAdminService.updateJournalSubType(id, request);
      setEditingJournalSubType(null);
      loadData();
    } catch (err: any) {
      setError(err.message || 'Failed to update journal sub type');
    }
  };

  const handleDeleteJournalSubType = async (id: number) => {
    if (!window.confirm('Are you sure you want to delete this journal sub type?')) return;

    try {
      await propertyAdminService.deleteJournalSubType(id);
      loadData();
    } catch (err: any) {
      setError(err.message || 'Failed to delete journal sub type');
    }
  };

  if (loading) {
    return <div className="text-center py-8">Loading...</div>;
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-bold text-gray-900">Lookup Data</h2>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded mb-4">
          {error}
        </div>
      )}

      {/* Tabs */}
      <div className="border-b border-gray-200 mb-6">
        <nav className="-mb-px flex space-x-8">
            <button
              onClick={() => setActiveTab('journal')}
              className={`whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm ${
                activeTab === 'journal'
                  ? 'border-blue-500 text-blue-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
              }`}
            >
              Journal Types
              {activeTab === 'journal' && (
                <button
                  onClick={(e) => {
                    e.stopPropagation();
                    setShowJournalModal(true);
                  }}
                  className="ml-4 bg-blue-600 text-white px-3 py-1 rounded text-xs hover:bg-blue-700"
                >
                  Create Journal Type
                </button>
              )}
            </button>
            <button
              onClick={() => setActiveTab('contact')}
              className={`whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm ${
                activeTab === 'contact'
                  ? 'border-blue-500 text-blue-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
              }`}
            >
              Contact Log Types
              {activeTab === 'contact' && (
                <button
                  onClick={(e) => {
                    e.stopPropagation();
                    setShowContactModal(true);
                  }}
                  className="ml-4 bg-blue-600 text-white px-3 py-1 rounded text-xs hover:bg-blue-700"
                >
                  Create Contact Log Type
                </button>
              )}
            </button>
          <button
            onClick={() => setActiveTab('tag')}
            className={`whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm ${
              activeTab === 'tag'
                ? 'border-blue-500 text-blue-600'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
            }`}
          >
            Tag Types
            {activeTab === 'tag' && (
              <button
                onClick={() => setShowTagModal(true)}
                className="ml-4 bg-blue-600 text-white px-3 py-1 rounded text-xs hover:bg-blue-700"
              >
                Create Tag Type
              </button>
            )}
          </button>
        </nav>
      </div>

      {/* Journal Types Tab */}
      {activeTab === 'journal' && (
        <div>
          <div className="bg-white shadow rounded-lg overflow-hidden">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Name</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Description</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Sub Types</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Actions</th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {journalTypes.map((type) => (
                  <tr key={type.journalTypeId}>
                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">{type.journalTypeName}</td>
                    <td className="px-6 py-4 text-sm text-gray-500">{type.description || '-'}</td>
                    <td className="px-6 py-4 text-sm text-gray-500">
                      <div className="space-y-2">
                        {type.subTypes.length > 0 ? (
                          type.subTypes.map((st) => (
                            <div key={st.journalSubTypeId} className="flex items-center justify-between bg-gray-50 px-2 py-1 rounded">
                              <span>{st.journalSubTypeName}</span>
                              <div className="space-x-2">
                                <button
                                  onClick={() => setEditingJournalSubType({ subType: st, journalTypeId: type.journalTypeId })}
                                  className="text-blue-600 hover:text-blue-900 text-xs"
                                >
                                  Edit
                                </button>
                                <button
                                  onClick={() => handleDeleteJournalSubType(st.journalSubTypeId)}
                                  className="text-red-600 hover:text-red-900 text-xs"
                                >
                                  Delete
                                </button>
                              </div>
                            </div>
                          ))
                        ) : (
                          <span className="text-gray-400">-</span>
                        )}
                        <button
                          onClick={() => {
                            setSelectedJournalTypeForSubType(type.journalTypeId);
                            setShowJournalSubTypeModal(true);
                          }}
                          className="text-blue-600 hover:text-blue-900 text-xs font-medium"
                        >
                          + Add Sub Type
                        </button>
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium space-x-2">
                      <button onClick={() => setEditingJournalType(type)} className="text-blue-600 hover:text-blue-900">Edit</button>
                      <button onClick={() => handleDeleteJournalType(type.journalTypeId)} className="text-red-600 hover:text-red-900">Delete</button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Create Journal Type Modal */}
          {showJournalModal && (
            <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
              <div className="relative top-20 mx-auto p-5 border w-96 shadow-lg rounded-md bg-white">
                <h3 className="text-lg font-bold mb-4">Create Journal Type</h3>
                <form onSubmit={handleCreateJournalType}>
                  <div className="mb-4">
                    <label className="block text-sm font-medium text-gray-700 mb-1">Journal Type Name *</label>
                    <input type="text" name="journalTypeName" required className="w-full px-3 py-2 border border-gray-300 rounded-md" />
                  </div>
                  <div className="mb-4">
                    <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
                    <textarea name="description" rows={3} className="w-full px-3 py-2 border border-gray-300 rounded-md" />
                  </div>
                  <div className="mb-4">
                    <label className="block text-sm font-medium text-gray-700 mb-1">Active</label>
                    <select name="isActive" className="w-full px-3 py-2 border border-gray-300 rounded-md">
                      <option value="">Not Set</option>
                      <option value="true">Active</option>
                      <option value="false">Inactive</option>
                    </select>
                  </div>
                  <div className="flex justify-end space-x-2">
                    <button type="button" onClick={() => setShowJournalModal(false)} className="px-4 py-2 border border-gray-300 rounded-md">Cancel</button>
                    <button type="submit" className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700">Create</button>
                  </div>
                </form>
              </div>
            </div>
          )}

          {/* Edit Journal Type Modal */}
          {editingJournalType && (
            <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
              <div className="relative top-20 mx-auto p-5 border w-96 shadow-lg rounded-md bg-white">
                <h3 className="text-lg font-bold mb-4">Edit Journal Type</h3>
                <form onSubmit={(e) => handleUpdateJournalType(editingJournalType.journalTypeId, e)}>
                  <div className="mb-4">
                    <label className="block text-sm font-medium text-gray-700 mb-1">Journal Type Name</label>
                    <input type="text" name="journalTypeName" defaultValue={editingJournalType.journalTypeName} className="w-full px-3 py-2 border border-gray-300 rounded-md" />
                  </div>
                  <div className="mb-4">
                    <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
                    <textarea name="description" rows={3} defaultValue={editingJournalType.description || ''} className="w-full px-3 py-2 border border-gray-300 rounded-md" />
                  </div>
                  <div className="mb-4">
                    <label className="block text-sm font-medium text-gray-700 mb-1">Active</label>
                    <select name="isActive" defaultValue={editingJournalType.isActive === true ? 'true' : editingJournalType.isActive === false ? 'false' : ''} className="w-full px-3 py-2 border border-gray-300 rounded-md">
                      <option value="">Not Set</option>
                      <option value="true">Active</option>
                      <option value="false">Inactive</option>
                    </select>
                  </div>
                  <div className="flex justify-end space-x-2">
                    <button type="button" onClick={() => setEditingJournalType(null)} className="px-4 py-2 border border-gray-300 rounded-md">Cancel</button>
                    <button type="submit" className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700">Update</button>
                  </div>
                </form>
              </div>
            </div>
          )}

          {/* Create Journal Sub Type Modal */}
          {showJournalSubTypeModal && selectedJournalTypeForSubType && (
            <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
              <div className="relative top-20 mx-auto p-5 border w-96 shadow-lg rounded-md bg-white">
                <h3 className="text-lg font-bold mb-4">Create Journal Sub Type</h3>
                <form onSubmit={handleCreateJournalSubType}>
                  <div className="mb-4">
                    <label className="block text-sm font-medium text-gray-700 mb-1">Journal Sub Type Name *</label>
                    <input type="text" name="journalSubTypeName" required className="w-full px-3 py-2 border border-gray-300 rounded-md" />
                  </div>
                  <div className="mb-4">
                    <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
                    <textarea name="description" rows={3} className="w-full px-3 py-2 border border-gray-300 rounded-md" />
                  </div>
                  <div className="mb-4">
                    <label className="block text-sm font-medium text-gray-700 mb-1">Active</label>
                    <select name="isActive" className="w-full px-3 py-2 border border-gray-300 rounded-md">
                      <option value="">Not Set</option>
                      <option value="true">Active</option>
                      <option value="false">Inactive</option>
                    </select>
                  </div>
                  <div className="flex justify-end space-x-2">
                    <button
                      type="button"
                      onClick={() => {
                        setShowJournalSubTypeModal(false);
                        setSelectedJournalTypeForSubType(null);
                      }}
                      className="px-4 py-2 border border-gray-300 rounded-md"
                    >
                      Cancel
                    </button>
                    <button type="submit" className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700">Create</button>
                  </div>
                </form>
              </div>
            </div>
          )}

          {/* Edit Journal Sub Type Modal */}
          {editingJournalSubType && (
            <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
              <div className="relative top-20 mx-auto p-5 border w-96 shadow-lg rounded-md bg-white">
                <h3 className="text-lg font-bold mb-4">Edit Journal Sub Type</h3>
                <form onSubmit={(e) => handleUpdateJournalSubType(editingJournalSubType.subType.journalSubTypeId, e)}>
                  <div className="mb-4">
                    <label className="block text-sm font-medium text-gray-700 mb-1">Journal Sub Type Name</label>
                    <input type="text" name="journalSubTypeName" defaultValue={editingJournalSubType.subType.journalSubTypeName} className="w-full px-3 py-2 border border-gray-300 rounded-md" />
                  </div>
                  <div className="mb-4">
                    <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
                    <textarea name="description" rows={3} defaultValue={editingJournalSubType.subType.description || ''} className="w-full px-3 py-2 border border-gray-300 rounded-md" />
                  </div>
                  <div className="mb-4">
                    <label className="block text-sm font-medium text-gray-700 mb-1">Active</label>
                    <select name="isActive" defaultValue={editingJournalSubType.subType.isActive === true ? 'true' : editingJournalSubType.subType.isActive === false ? 'false' : ''} className="w-full px-3 py-2 border border-gray-300 rounded-md">
                      <option value="">Not Set</option>
                      <option value="true">Active</option>
                      <option value="false">Inactive</option>
                    </select>
                  </div>
                  <div className="flex justify-end space-x-2">
                    <button type="button" onClick={() => setEditingJournalSubType(null)} className="px-4 py-2 border border-gray-300 rounded-md">Cancel</button>
                    <button type="submit" className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700">Update</button>
                  </div>
                </form>
              </div>
            </div>
          )}
        </div>
      )}

      {/* Contact Log Types Tab */}
      {activeTab === 'contact' && (
        <div>
          <div className="bg-white shadow rounded-lg overflow-hidden">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Name</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Description</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Actions</th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {contactLogTypes.map((type) => (
                  <tr key={type.contactLogTypeId}>
                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">{type.contactLogTypeName}</td>
                    <td className="px-6 py-4 text-sm text-gray-500">{type.description || '-'}</td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium space-x-2">
                      <button onClick={() => setEditingContactLogType(type)} className="text-blue-600 hover:text-blue-900">Edit</button>
                      <button onClick={() => handleDeleteContactLogType(type.contactLogTypeId)} className="text-red-600 hover:text-red-900">Delete</button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Create Contact Log Type Modal */}
          {showContactModal && (
            <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
              <div className="relative top-20 mx-auto p-5 border w-96 shadow-lg rounded-md bg-white">
                <h3 className="text-lg font-bold mb-4">Create Contact Log Type</h3>
                <form onSubmit={handleCreateContactLogType}>
                  <div className="mb-4">
                    <label className="block text-sm font-medium text-gray-700 mb-1">Contact Log Type Name *</label>
                    <input type="text" name="contactLogTypeName" required className="w-full px-3 py-2 border border-gray-300 rounded-md" />
                  </div>
                  <div className="mb-4">
                    <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
                    <textarea name="description" rows={3} className="w-full px-3 py-2 border border-gray-300 rounded-md" />
                  </div>
                  <div className="mb-4">
                    <label className="block text-sm font-medium text-gray-700 mb-1">Active</label>
                    <select name="isActive" className="w-full px-3 py-2 border border-gray-300 rounded-md">
                      <option value="">Not Set</option>
                      <option value="true">Active</option>
                      <option value="false">Inactive</option>
                    </select>
                  </div>
                  <div className="flex justify-end space-x-2">
                    <button type="button" onClick={() => setShowContactModal(false)} className="px-4 py-2 border border-gray-300 rounded-md">Cancel</button>
                    <button type="submit" className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700">Create</button>
                  </div>
                </form>
              </div>
            </div>
          )}

          {/* Edit Contact Log Type Modal */}
          {editingContactLogType && (
            <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
              <div className="relative top-20 mx-auto p-5 border w-96 shadow-lg rounded-md bg-white">
                <h3 className="text-lg font-bold mb-4">Edit Contact Log Type</h3>
                <form onSubmit={(e) => handleUpdateContactLogType(editingContactLogType.contactLogTypeId, e)}>
                  <div className="mb-4">
                    <label className="block text-sm font-medium text-gray-700 mb-1">Contact Log Type Name</label>
                    <input type="text" name="contactLogTypeName" defaultValue={editingContactLogType.contactLogTypeName} className="w-full px-3 py-2 border border-gray-300 rounded-md" />
                  </div>
                  <div className="mb-4">
                    <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
                    <textarea name="description" rows={3} defaultValue={editingContactLogType.description || ''} className="w-full px-3 py-2 border border-gray-300 rounded-md" />
                  </div>
                  <div className="mb-4">
                    <label className="block text-sm font-medium text-gray-700 mb-1">Active</label>
                    <select name="isActive" defaultValue={editingContactLogType.isActive === true ? 'true' : editingContactLogType.isActive === false ? 'false' : ''} className="w-full px-3 py-2 border border-gray-300 rounded-md">
                      <option value="">Not Set</option>
                      <option value="true">Active</option>
                      <option value="false">Inactive</option>
                    </select>
                  </div>
                  <div className="flex justify-end space-x-2">
                    <button type="button" onClick={() => setEditingContactLogType(null)} className="px-4 py-2 border border-gray-300 rounded-md">Cancel</button>
                    <button type="submit" className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700">Update</button>
                  </div>
                </form>
              </div>
            </div>
          )}
        </div>
      )}

      {/* Tag Types Tab */}
      {activeTab === 'tag' && (
        <div>
          <div className="bg-white shadow rounded-lg overflow-hidden">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Name</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Color</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Description</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Actions</th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {tagTypes.map((type) => (
                  <tr key={type.tagTypeId}>
                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">{type.tagTypeName}</td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      {type.color && (
                        <span className="px-2 py-1 rounded text-xs text-white" style={{ backgroundColor: type.color }}>
                          {type.color}
                        </span>
                      )}
                      {!type.color && <span className="text-gray-400">-</span>}
                    </td>
                    <td className="px-6 py-4 text-sm text-gray-500">{type.description || '-'}</td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium space-x-2">
                      <button onClick={() => setEditingTagType(type)} className="text-blue-600 hover:text-blue-900">Edit</button>
                      <button onClick={() => handleDeleteTagType(type.tagTypeId)} className="text-red-600 hover:text-red-900">Delete</button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Create Tag Type Modal */}
          {showTagModal && (
            <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
              <div className="relative top-20 mx-auto p-5 border w-96 shadow-lg rounded-md bg-white">
                <h3 className="text-lg font-bold mb-4">Create Tag Type</h3>
                <form onSubmit={handleCreateTagType}>
                  <div className="mb-4">
                    <label className="block text-sm font-medium text-gray-700 mb-1">Tag Type Name *</label>
                    <input type="text" name="tagTypeName" required className="w-full px-3 py-2 border border-gray-300 rounded-md" />
                  </div>
                  <div className="mb-4">
                    <label className="block text-sm font-medium text-gray-700 mb-1">Color</label>
                    <input type="color" name="color" className="w-full h-10 border border-gray-300 rounded-md" />
                  </div>
                  <div className="mb-4">
                    <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
                    <textarea name="description" rows={3} className="w-full px-3 py-2 border border-gray-300 rounded-md" />
                  </div>
                  <div className="mb-4">
                    <label className="block text-sm font-medium text-gray-700 mb-1">Active</label>
                    <select name="isActive" className="w-full px-3 py-2 border border-gray-300 rounded-md">
                      <option value="">Not Set</option>
                      <option value="true">Active</option>
                      <option value="false">Inactive</option>
                    </select>
                  </div>
                  <div className="flex justify-end space-x-2">
                    <button type="button" onClick={() => setShowTagModal(false)} className="px-4 py-2 border border-gray-300 rounded-md">Cancel</button>
                    <button type="submit" className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700">Create</button>
                  </div>
                </form>
              </div>
            </div>
          )}

          {/* Edit Tag Type Modal */}
          {editingTagType && (
            <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
              <div className="relative top-20 mx-auto p-5 border w-96 shadow-lg rounded-md bg-white">
                <h3 className="text-lg font-bold mb-4">Edit Tag Type</h3>
                <form onSubmit={(e) => handleUpdateTagType(editingTagType.tagTypeId, e)}>
                  <div className="mb-4">
                    <label className="block text-sm font-medium text-gray-700 mb-1">Tag Type Name</label>
                    <input type="text" name="tagTypeName" defaultValue={editingTagType.tagTypeName} className="w-full px-3 py-2 border border-gray-300 rounded-md" />
                  </div>
                  <div className="mb-4">
                    <label className="block text-sm font-medium text-gray-700 mb-1">Color</label>
                    <input type="color" name="color" defaultValue={editingTagType.color || '#000000'} className="w-full h-10 border border-gray-300 rounded-md" />
                  </div>
                  <div className="mb-4">
                    <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
                    <textarea name="description" rows={3} defaultValue={editingTagType.description || ''} className="w-full px-3 py-2 border border-gray-300 rounded-md" />
                  </div>
                  <div className="mb-4">
                    <label className="block text-sm font-medium text-gray-700 mb-1">Active</label>
                    <select name="isActive" defaultValue={editingTagType.isActive === true ? 'true' : editingTagType.isActive === false ? 'false' : ''} className="w-full px-3 py-2 border border-gray-300 rounded-md">
                      <option value="">Not Set</option>
                      <option value="true">Active</option>
                      <option value="false">Inactive</option>
                    </select>
                  </div>
                  <div className="flex justify-end space-x-2">
                    <button type="button" onClick={() => setEditingTagType(null)} className="px-4 py-2 border border-gray-300 rounded-md">Cancel</button>
                    <button type="submit" className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700">Update</button>
                  </div>
                </form>
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
};

export default Lookups;
