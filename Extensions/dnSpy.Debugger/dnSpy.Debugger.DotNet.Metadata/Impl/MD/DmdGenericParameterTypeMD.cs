﻿/*
    Copyright (C) 2014-2017 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using dnlib.DotNet.MD;

namespace dnSpy.Debugger.DotNet.Metadata.Impl.MD {
	sealed class DmdGenericParameterTypeMD : DmdGenericParameterType {
		readonly DmdEcma335MetadataReader reader;

		public DmdGenericParameterTypeMD(DmdEcma335MetadataReader reader, uint rid, DmdType declaringType, string name, int position, DmdGenericParameterAttributes attributes, IList<DmdCustomModifier> customModifiers)
			: base(rid, declaringType, name, position, attributes, customModifiers) =>
			this.reader = reader ?? throw new ArgumentNullException(nameof(reader));

		public DmdGenericParameterTypeMD(DmdEcma335MetadataReader reader, uint rid, DmdMethodBase declaringMethod, string name, int position, DmdGenericParameterAttributes attributes, IList<DmdCustomModifier> customModifiers)
			: base(rid, declaringMethod, name, position, attributes, customModifiers) =>
			this.reader = reader ?? throw new ArgumentNullException(nameof(reader));

		protected override DmdType[] CreateGenericParameterConstraints_NoLock() {
			var ridList = reader.Metadata.GetGenericParamConstraintRidList(Rid);
			if (ridList.Count == 0)
				return null;

			IList<DmdType> genericTypeArguments, genericMethodArguments;
			if ((object)DeclaringMethod != null) {
				genericTypeArguments = DeclaringMethod.DeclaringType.GetReadOnlyGenericArguments();
				genericMethodArguments = DeclaringMethod.GetReadOnlyGenericArguments();
			}
			else {
				genericTypeArguments = DeclaringType.GetReadOnlyGenericArguments();
				genericMethodArguments = null;
			}

			var gpcList = new DmdType[ridList.Count];
			for (int i = 0; i < ridList.Count; i++) {
				uint rid = ridList[i];
				var row = reader.TablesStream.ReadGenericParamConstraintRow(rid);
				if (!CodedToken.TypeDefOrRef.Decode(row.Constraint, out uint token))
					return null;
				var type = Module.ResolveType((int)token, genericTypeArguments, genericMethodArguments, throwOnError: false);
				if ((object)type == null)
					return null;
				gpcList[i] = type;
			}
			return gpcList;
		}

		DmdGenericParameterTypeMD Clone(IList<DmdCustomModifier> customModifiers) =>
			(object)DeclaringMethod != null ?
			new DmdGenericParameterTypeMD(reader, Rid, DeclaringMethod, Name, GenericParameterPosition, GenericParameterAttributes, customModifiers) :
			new DmdGenericParameterTypeMD(reader, Rid, DeclaringType, Name, GenericParameterPosition, GenericParameterAttributes, customModifiers);

		// Don't intern these since only the generic parameter position is checked and not the decl type / method
		public override DmdType WithCustomModifiers(IList<DmdCustomModifier> customModifiers) => Clone(customModifiers);
		public override DmdType WithoutCustomModifiers() => GetCustomModifiers().Count == 0 ? this : Clone(null);
	}
}