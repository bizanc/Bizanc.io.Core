using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Bizanc.io.Matching.Core.Util
{
    public class ImutList<T> : IEnumerable<T>
    {
        enum OperationEnum
        {
            None,
            Add,
            Set,
            Delete
        }

        private ImutList<T> baseList;

        private List<T> myList;

        private bool initialized = false;

        private OperationEnum op;
        private int affectedIndex;
        private T opResult;

        private int opCount = 0;

        public ImutList(List<T> baseList)
        {
            this.myList = baseList;
            initialized = true;
        }

        public ImutList()
        {
            this.myList = new List<T>();
            initialized = true;
        }


        private ImutList(ImutList<T> baseList, OperationEnum op, int index, T result, int opCount)
        {
            this.baseList = baseList;
            this.op = op;
            this.affectedIndex = index;
            this.opResult = result;
            this.opCount = opCount;

            if (opCount >= 1000)
            {
                this.initialized = true;
                myList = this.ToList();
                this.baseList = null;
                this.op = OperationEnum.None;
                this.affectedIndex = 0;
                this.opResult = default(T);
                this.opCount = 0;
            }
        }

        public T this[int index]
        {
            get
            {
                if (op != OperationEnum.None)
                {
                    if (index == affectedIndex)
                    {
                        if (op == OperationEnum.Add || op == OperationEnum.Set)
                            return opResult;
                        else
                        {
                            if (initialized)
                                return myList[index];
                            else
                                return baseList[index];
                        }
                    }
                    else
                    {
                        if (op == OperationEnum.Add)
                            if (index > affectedIndex)
                            {
                                if (initialized)
                                    return myList[index + 1];
                                else
                                    return baseList[index + 1];
                            }
                            else
                            {
                                if (initialized)
                                    return myList[index];
                                else
                                    return baseList[index];
                            }
                        else if (op == OperationEnum.Set)
                        {
                            if (initialized)
                                return myList[index];
                            else
                                return baseList[index];
                        }
                        else
                        if (index > affectedIndex)
                        {
                            if (initialized)
                                return myList[index - 1];
                            else
                                return baseList[index - 1];
                        }
                        else
                        {
                            if (initialized)
                                return myList[index];
                            else
                                return baseList[index];
                        }
                    }
                }
                else
                {
                    if (initialized)
                        return myList[index];
                    else
                        return baseList[index];
                }


            }
            set { }
        }

        public int Count
        {
            get
            {
                var result = 0;

                if (initialized)
                    result = myList.Count;
                else
                    result = baseList.Count;

                if (op == OperationEnum.Add || op == OperationEnum.Set)
                    return result + 1;
                else if (op == OperationEnum.Delete)
                    return result - 1;

                return result;
            }
        }

        public bool IsReadOnly { get { return true; } }

        public ImutList<T> Add(T item)
        {
            return new ImutList<T>(this, OperationEnum.Add, Count - 1, item, opCount + 1);
        }

        public ImutList<T> AddRange(IEnumerable<T> range)
        {
            var result = this;

            foreach (var t in range)
            {
                result = result.Add(t);
            }

            return result;
        }

        public bool Contains(T item)
        {
            if (opResult != null && opResult.Equals(item))
                return true;

            if (initialized)
                return myList.Contains(item);
            else
                return baseList.Contains(item);
        }

        public int IndexOf(T item)
        {
            if (opResult != null && opResult.Equals(item))
                return affectedIndex;

            if (initialized)
                return myList.IndexOf(item);
            else
                return baseList.IndexOf(item);
        }

        public ImutList<T> Insert(int index, T item)
        {
            return new ImutList<T>(this, OperationEnum.Add, index, item, opCount + 1);
        }

        public ImutList<T> Remove(T item)
        {
            var index = IndexOf(item);
            if (index >= 0)
                return new ImutList<T>(this, OperationEnum.Delete, index, initialized ? myList[index] : baseList[index], opCount + 1);

            return this;
        }

        public ImutList<T> RemoveAt(int index)
        {
            if (index < Count)
                return new ImutList<T>(this, OperationEnum.Delete, index, initialized ? myList[index] : baseList[index], opCount + 1);

            return this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new ImutListEnumerator<T>(this);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new ImutListEnumerator<T>(this);
        }
    }

    public class ImutListEnumerator<T> : IEnumerator<T>, IEnumerator
    {
        private ImutList<T> list;
        private int index = 0;

        public ImutListEnumerator(ImutList<T> list)
        {
            this.list = list;
        }

        public T Current { get { return list[index]; } }

        object IEnumerator.Current { get { return list[index]; } }

        public void Dispose()
        {

        }

        public bool MoveNext()
        {
            index++;
            return index < list.Count;
        }

        public void Reset()
        {
            index = 0;
        }
    }
}