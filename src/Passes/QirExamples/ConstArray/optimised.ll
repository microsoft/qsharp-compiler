; ModuleID = 'combined.ll'
source_filename = "llvm-link"
target datalayout = "e-m:o-p270:32:32-p271:32:32-p272:64:64-i64:64-f80:128-n8:16:32:64-S128"
target triple = "x86_64-apple-macosx11.0.0"

%"class.std::__1::unordered_map" = type { %"class.std::__1::__hash_table" }
%"class.std::__1::__hash_table" = type <{ %"class.std::__1::unique_ptr", %"class.std::__1::__compressed_pair.4", %"class.std::__1::__compressed_pair.1", %"class.std::__1::__compressed_pair.11", [4 x i8] }>
%"class.std::__1::unique_ptr" = type { %"class.std::__1::__compressed_pair" }
%"class.std::__1::__compressed_pair" = type { %"struct.std::__1::__compressed_pair_elem", %"struct.std::__1::__compressed_pair_elem.0" }
%"struct.std::__1::__compressed_pair_elem" = type { %"struct.std::__1::__hash_node_base"** }
%"struct.std::__1::__hash_node_base" = type { %"struct.std::__1::__hash_node_base"* }
%"struct.std::__1::__compressed_pair_elem.0" = type { %"class.std::__1::__bucket_list_deallocator" }
%"class.std::__1::__bucket_list_deallocator" = type { %"class.std::__1::__compressed_pair.1" }
%"class.std::__1::__compressed_pair.4" = type { %"struct.std::__1::__compressed_pair_elem.5" }
%"struct.std::__1::__compressed_pair_elem.5" = type { %"struct.std::__1::__hash_node_base" }
%"class.std::__1::__compressed_pair.1" = type { %"struct.std::__1::__compressed_pair_elem.2" }
%"struct.std::__1::__compressed_pair_elem.2" = type { i64 }
%"class.std::__1::__compressed_pair.11" = type { %"struct.std::__1::__compressed_pair_elem.12" }
%"struct.std::__1::__compressed_pair_elem.12" = type { float }
%class.Array = type { i64, i64, i8* }
%String = type opaque

@alias_count = global %"class.std::__1::unordered_map" zeroinitializer, align 8
@ref_count = global %"class.std::__1::unordered_map" zeroinitializer, align 8
@llvm.global_ctors = appending global [1 x { i32, void ()*, i8* }] [{ i32, void ()*, i8* } { i32 65535, void ()* @_GLOBAL__sub_I_runtime.cpp, i8* null }]
@__dso_handle = external hidden global i8

define internal void @_GLOBAL__sub_I_runtime.cpp() personality i32 (...)* @__gxx_personality_v0 {
  tail call void @llvm.memset.p0i8.i64(i8* nonnull align 8 dereferenceable(32) bitcast (%"class.std::__1::unordered_map"* @alias_count to i8*), i8 0, i64 32, i1 false)
  store i32 1065353216, i32* bitcast (%"class.std::__1::__compressed_pair.11"* getelementptr inbounds (%"class.std::__1::unordered_map", %"class.std::__1::unordered_map"* @alias_count, i64 0, i32 0, i32 3) to i32*), align 8
  %1 = tail call i32 @__cxa_atexit(void (i8*)* bitcast (void (%"class.std::__1::unordered_map"*)* @_ZNSt3__113unordered_mapIP5ArrayiNS_4hashIS2_EENS_8equal_toIS2_EENS_9allocatorINS_4pairIKS2_iEEEEED1Ev to void (i8*)*), i8* bitcast (%"class.std::__1::unordered_map"* @alias_count to i8*), i8* nonnull @__dso_handle)
  tail call void @llvm.memset.p0i8.i64(i8* nonnull align 8 dereferenceable(32) bitcast (%"class.std::__1::unordered_map"* @ref_count to i8*), i8 0, i64 32, i1 false)
  store i32 1065353216, i32* bitcast (%"class.std::__1::__compressed_pair.11"* getelementptr inbounds (%"class.std::__1::unordered_map", %"class.std::__1::unordered_map"* @ref_count, i64 0, i32 0, i32 3) to i32*), align 8
  %2 = tail call i32 @__cxa_atexit(void (i8*)* bitcast (void (%"class.std::__1::unordered_map"*)* @_ZNSt3__113unordered_mapIP5ArrayiNS_4hashIS2_EENS_8equal_toIS2_EENS_9allocatorINS_4pairIKS2_iEEEEED1Ev to void (i8*)*), i8* bitcast (%"class.std::__1::unordered_map"* @ref_count to i8*), i8* nonnull @__dso_handle)
  ret void
}

define linkonce_odr hidden void @_ZNSt3__113unordered_mapIP5ArrayiNS_4hashIS2_EENS_8equal_toIS2_EENS_9allocatorINS_4pairIKS2_iEEEEED1Ev(%"class.std::__1::unordered_map"* %0) unnamed_addr personality i32 (...)* @__gxx_personality_v0 {
  %2 = getelementptr inbounds %"class.std::__1::unordered_map", %"class.std::__1::unordered_map"* %0, i64 0, i32 0, i32 1, i32 0, i32 0, i32 0
  %3 = load %"struct.std::__1::__hash_node_base"*, %"struct.std::__1::__hash_node_base"** %2, align 8
  br label %_ZNSt3__116allocator_traitsINS_9allocatorINS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEE10deallocateERS9_PS8_m.exit.i.i.i.i

_ZNSt3__116allocator_traitsINS_9allocatorINS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEE10deallocateERS9_PS8_m.exit.i.i.i.i: ; preds = %4, %1
  %.0.i.i.i.i = phi %"struct.std::__1::__hash_node_base"* [ %3, %1 ], [ %6, %4 ]
  %.not.i.i.i.i = icmp eq %"struct.std::__1::__hash_node_base"* %.0.i.i.i.i, null
  br i1 %.not.i.i.i.i, label %_ZNSt3__112__hash_tableINS_17__hash_value_typeIP5ArrayiEENS_22__unordered_map_hasherIS3_S4_NS_4hashIS3_EELb1EEENS_21__unordered_map_equalIS3_S4_NS_8equal_toIS3_EELb1EEENS_9allocatorIS4_EEE17__deallocate_nodeEPNS_16__hash_node_baseIPNS_11__hash_nodeIS4_PvEEEE.exit.i.i.i, label %4

4:                                                ; preds = %_ZNSt3__116allocator_traitsINS_9allocatorINS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEE10deallocateERS9_PS8_m.exit.i.i.i.i
  %5 = getelementptr inbounds %"struct.std::__1::__hash_node_base", %"struct.std::__1::__hash_node_base"* %.0.i.i.i.i, i64 0, i32 0
  %6 = load %"struct.std::__1::__hash_node_base"*, %"struct.std::__1::__hash_node_base"** %5, align 8
  %7 = bitcast %"struct.std::__1::__hash_node_base"* %.0.i.i.i.i to i8*
  invoke void @_ZdlPv(i8* nonnull %7)
          to label %_ZNSt3__116allocator_traitsINS_9allocatorINS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEE10deallocateERS9_PS8_m.exit.i.i.i.i unwind label %8

8:                                                ; preds = %4
  %9 = landingpad { i8*, i32 }
          catch i8* null
  %10 = extractvalue { i8*, i32 } %9, 0
  tail call void @__clang_call_terminate(i8* %10)
  unreachable

_ZNSt3__112__hash_tableINS_17__hash_value_typeIP5ArrayiEENS_22__unordered_map_hasherIS3_S4_NS_4hashIS3_EELb1EEENS_21__unordered_map_equalIS3_S4_NS_8equal_toIS3_EELb1EEENS_9allocatorIS4_EEE17__deallocate_nodeEPNS_16__hash_node_baseIPNS_11__hash_nodeIS4_PvEEEE.exit.i.i.i: ; preds = %_ZNSt3__116allocator_traitsINS_9allocatorINS_11__hash_nodeINS_17__hash_value_typeIP5ArrayiEEPvEEEEE10deallocateERS9_PS8_m.exit.i.i.i.i
  %11 = getelementptr inbounds %"class.std::__1::unordered_map", %"class.std::__1::unordered_map"* %0, i64 0, i32 0, i32 0, i32 0, i32 0, i32 0
  %12 = load %"struct.std::__1::__hash_node_base"**, %"struct.std::__1::__hash_node_base"*** %11, align 8
  store %"struct.std::__1::__hash_node_base"** null, %"struct.std::__1::__hash_node_base"*** %11, align 8
  %.not.i.i.i.i.i.i = icmp eq %"struct.std::__1::__hash_node_base"** %12, null
  br i1 %.not.i.i.i.i.i.i, label %_ZNSt3__113unordered_mapIP5ArrayiNS_4hashIS2_EENS_8equal_toIS2_EENS_9allocatorINS_4pairIKS2_iEEEEED2Ev.exit, label %13

13:                                               ; preds = %_ZNSt3__112__hash_tableINS_17__hash_value_typeIP5ArrayiEENS_22__unordered_map_hasherIS3_S4_NS_4hashIS3_EELb1EEENS_21__unordered_map_equalIS3_S4_NS_8equal_toIS3_EELb1EEENS_9allocatorIS4_EEE17__deallocate_nodeEPNS_16__hash_node_baseIPNS_11__hash_nodeIS4_PvEEEE.exit.i.i.i
  %14 = bitcast %"struct.std::__1::__hash_node_base"** %12 to i8*
  invoke void @_ZdlPv(i8* nonnull %14)
          to label %_ZNSt3__113unordered_mapIP5ArrayiNS_4hashIS2_EENS_8equal_toIS2_EENS_9allocatorINS_4pairIKS2_iEEEEED2Ev.exit unwind label %15

15:                                               ; preds = %13
  %16 = landingpad { i8*, i32 }
          catch i8* null
  %17 = extractvalue { i8*, i32 } %16, 0
  tail call void @__clang_call_terminate(i8* %17)
  unreachable

_ZNSt3__113unordered_mapIP5ArrayiNS_4hashIS2_EENS_8equal_toIS2_EENS_9allocatorINS_4pairIKS2_iEEEEED2Ev.exit: ; preds = %_ZNSt3__112__hash_tableINS_17__hash_value_typeIP5ArrayiEENS_22__unordered_map_hasherIS3_S4_NS_4hashIS3_EELb1EEENS_21__unordered_map_equalIS3_S4_NS_8equal_toIS3_EELb1EEENS_9allocatorIS4_EEE17__deallocate_nodeEPNS_16__hash_node_baseIPNS_11__hash_nodeIS4_PvEEEE.exit.i.i.i, %13
  ret void
}

; Function Attrs: nofree
declare i32 @__cxa_atexit(void (i8*)*, i8*, i8*) local_unnamed_addr #0

declare i32 @__gxx_personality_v0(...)

define linkonce_odr hidden void @__clang_call_terminate(i8* %0) local_unnamed_addr {
  %2 = tail call i8* @__cxa_begin_catch(i8* %0)
  tail call void @_ZSt9terminatev()
  unreachable
}

declare i8* @__cxa_begin_catch(i8*) local_unnamed_addr

declare void @_ZSt9terminatev() local_unnamed_addr

declare void @_ZdlPv(i8*) local_unnamed_addr

define nonnull %class.Array* @__quantum__rt__array_create_1d(i32 %0, i64 %1) local_unnamed_addr personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*) {
  %3 = tail call noalias nonnull dereferenceable(24) i8* @_Znwm(i64 24)
  %4 = sext i32 %0 to i64
  %5 = bitcast i8* %3 to i64*
  store i64 %4, i64* %5, align 8
  %6 = getelementptr inbounds i8, i8* %3, i64 8
  %7 = bitcast i8* %6 to i64*
  store i64 %1, i64* %7, align 8
  %8 = mul nsw i64 %4, %1
  %9 = invoke noalias nonnull i8* @_Znam(i64 %8)
          to label %10 unwind label %14

10:                                               ; preds = %2
  %11 = bitcast i8* %3 to %class.Array*
  %12 = getelementptr inbounds i8, i8* %3, i64 16
  %13 = bitcast i8* %12 to i8**
  store i8* %9, i8** %13, align 8
  ret %class.Array* %11

14:                                               ; preds = %2
  %15 = landingpad { i8*, i32 }
          cleanup
  tail call void @_ZdlPv(i8* nonnull %3)
  resume { i8*, i32 } %15
}

; Function Attrs: nofree
declare noalias nonnull i8* @_Znwm(i64) local_unnamed_addr #0

; Function Attrs: nofree
declare noalias nonnull i8* @_Znam(i64) local_unnamed_addr #0

; Function Attrs: norecurse nounwind readonly
define i8* @__quantum__rt__array_get_element_ptr_1d(%class.Array* nocapture readonly %0, i64 %1) local_unnamed_addr #1 {
  %3 = getelementptr inbounds %class.Array, %class.Array* %0, i64 0, i32 2
  %4 = load i8*, i8** %3, align 8
  %5 = getelementptr inbounds %class.Array, %class.Array* %0, i64 0, i32 0
  %6 = load i64, i64* %5, align 8
  %7 = mul nsw i64 %6, %1
  %8 = getelementptr inbounds i8, i8* %4, i64 %7
  ret i8* %8
}

define void @__quantum__rt__qubit_release_array(%class.Array* %0) local_unnamed_addr {
  %2 = icmp eq %class.Array* %0, null
  br i1 %2, label %9, label %3

3:                                                ; preds = %1
  %4 = getelementptr inbounds %class.Array, %class.Array* %0, i64 0, i32 2
  %5 = load i8*, i8** %4, align 8
  %6 = icmp eq i8* %5, null
  br i1 %6, label %_ZN5ArrayD1Ev.exit, label %7

7:                                                ; preds = %3
  tail call void @_ZdaPv(i8* nonnull %5)
  br label %_ZN5ArrayD1Ev.exit

_ZN5ArrayD1Ev.exit:                               ; preds = %3, %7
  %8 = bitcast %class.Array* %0 to i8*
  tail call void @_ZdlPv(i8* %8)
  br label %9

9:                                                ; preds = %_ZN5ArrayD1Ev.exit, %1
  ret void
}

declare void @_ZdaPv(i8*) local_unnamed_addr

; Function Attrs: norecurse nounwind readnone
define void @__quantum__rt__array_update_alias_count(%class.Array* nocapture %0, i32 %1) local_unnamed_addr #2 {
  ret void
}

; Function Attrs: norecurse nounwind readnone
define void @__quantum__rt__array_update_reference_count(%class.Array* nocapture %0, i32 %1) local_unnamed_addr #2 {
  ret void
}

; Function Attrs: norecurse nounwind readnone
define %class.Array* @__quantum__rt__array_copy(%class.Array* readnone %0, i1 zeroext %1) local_unnamed_addr #2 {
  ret %class.Array* %0
}

; Function Attrs: norecurse nounwind readnone
define i64 @ConstArrayReduction__Main__Interop() local_unnamed_addr #3 personality i32 (...)* @__gxx_personality_v0 {
entry:
  ret i64 1337
}

define void @ConstArrayReduction__Main() local_unnamed_addr #4 personality i32 (...)* @__gxx_personality_v0 {
entry:
  %0 = tail call %String* @__quantum__rt__int_to_string(i64 1337)
  tail call void @__quantum__rt__message(%String* %0)
  tail call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)
  ret void
}

declare %String* @__quantum__rt__int_to_string(i64) local_unnamed_addr

declare void @__quantum__rt__message(%String*) local_unnamed_addr

declare void @__quantum__rt__string_update_reference_count(%String*, i32) local_unnamed_addr

; Function Attrs: argmemonly nounwind willreturn writeonly
declare void @llvm.memset.p0i8.i64(i8* nocapture writeonly, i8, i64, i1 immarg) #5

attributes #0 = { nofree }
attributes #1 = { norecurse nounwind readonly }
attributes #2 = { norecurse nounwind readnone }
attributes #3 = { norecurse nounwind readnone "InteropFriendly" }
attributes #4 = { "EntryPoint" }
attributes #5 = { argmemonly nounwind willreturn writeonly }

!llvm.ident = !{!0}
!llvm.module.flags = !{!1, !2}

!0 = !{!"Homebrew clang version 11.1.0"}
!1 = !{i32 1, !"wchar_size", i32 4}
!2 = !{i32 7, !"PIC Level", i32 2}
